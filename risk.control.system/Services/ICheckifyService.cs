using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using System.Net.Http;

using risk.control.system.Models.ViewModel;
using risk.control.system.Controllers.Api;
using risk.control.system.Data;
using risk.control.system.AppConstant;
using System.IO;
using Amazon.Textract;
using System.Xml;
using System.Text.RegularExpressions;
using risk.control.system.Controllers.Api.Claims;
using static Google.Apis.Requests.BatchRequest;
using System.Threading.Tasks;

namespace risk.control.system.Services;

public interface IICheckifyService
{
    Task<AppiCheckifyResponse> GetFaceId(FaceData data);
    Task<AppiCheckifyResponse> GetDocumentId(DocumentData data);
    Task<AppiCheckifyResponse> GetPassportId(DocumentData data);
    Task<AppiCheckifyResponse> GetAudio(AudioData data);
    Task<AppiCheckifyResponse> GetVideo(VideoData data);
    Task<bool> WhitelistIP(IPWhitelistRequest request);
}

public class ICheckifyService : IICheckifyService
{
    private static Regex panRegex = new Regex(@"[A-Z]{5}\d{4}[A-Z]{1}");
    private static Regex passportRegex = new Regex(@"[A-Z]{1,2}[0-9]{6,7}");
    private static Regex dateOfBirthRegex = new Regex(@"^(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[0-2])/([12]\d{3})$");
    private static string panNumber2Find = "Permanent Account Number";
    private static string passportNumber2Find = "Passport No.";
    private static string dateOfBirth2Find = "Date Of Birth";
    private static string audioS3FolderName = "icheckify-audio-transcript";
    private readonly ApplicationDbContext _context;
    private readonly IGoogleApi googleApi;
    private readonly IGoogleMaskHelper googleHelper;
    private readonly IHttpClientService httpClientService;
    private readonly IClaimsService claimsService;
    private readonly IFaceMatchService faceMatchService;
    private readonly IWebHostEnvironment webHostEnvironment;

    private static HttpClient httpClient = new();

    //test PAN FNLPM8635N
    public ICheckifyService(ApplicationDbContext context, IGoogleApi googleApi,
        IGoogleMaskHelper googleHelper, IHttpClientService httpClientService,
        IClaimsService claimsService,
        IFaceMatchService faceMatchService,
        IWebHostEnvironment webHostEnvironment)
    {
        this._context = context;
        this.googleApi = googleApi;
        this.googleHelper = googleHelper;
        this.httpClientService = httpClientService;
        this.claimsService = claimsService;
        this.faceMatchService = faceMatchService;
        this.webHostEnvironment = webHostEnvironment;
    }
    public async Task<bool> WhitelistIP(IPWhitelistRequest request)
    {
        var company = _context.ClientCompany.FirstOrDefault(c => c.Email == request.Domain);
        if (company == null)
        {
            return false;
        }

        company.WhitelistIpAddress += ";" + request.IpAddress;
        _context.ClientCompany.Update(company);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AppiCheckifyResponse> GetFaceId(FaceData data)
    {
        try
        {
            var claim = claimsService.GetClaims().Include(c => c.AgencyReport).ThenInclude(c => c.DigitalIdReport).FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            if (claim.AgencyReport == null)
            {
                claim.AgencyReport = new AgencyReport();
            }
            claim.AgencyReport.AgentEmail = data.Email;
            claim.AgencyReport.DigitalIdReport.Updated = DateTime.Now;
            claim.AgencyReport.DigitalIdReport.UpdatedBy = data.Email;
            claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime = DateTime.Now;
            claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat = data.LocationLongLat;
            var longLat = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
            var latitude = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";

            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl = url;

            #region FACE IMAGE PROCESSING

            byte[]? registeredImage = null;

            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                registeredImage = claim.CustomerDetail.ProfilePicture;
            }
            if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
            {
                registeredImage = claim.BeneficiaryDetail.ProfilePicture;
            }

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, data.LocationImage);
            var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            var addressTask = httpClientService.GetRawAddress(latitude, longitude);
            #endregion FACE IMAGE PROCESSING

            await Task.WhenAll(faceMatchTask, addressTask, weatherTask);

            var (confidence, compressImage) = await faceMatchTask;
            var address = await addressTask;
            var weatherData = await weatherTask;

            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";

            claim.AgencyReport.DigitalIdReport.DigitalIdImageData = weatherCustomData;
            claim.AgencyReport.DigitalIdReport.DigitalIdImage = compressImage;
            claim.AgencyReport.DigitalIdReport.DigitalIdImageMatchConfidence = confidence;
            claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationAddress = address;
            claim.AgencyReport.DigitalIdReport.MatchExecuted = true;

            var updateClaim = _context.ClaimsInvestigation.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = updateClaim.Entity.AgencyReport?.DigitalIdReport?.DigitalIdImage != null ?
                Convert.ToBase64String(claim.AgencyReport?.DigitalIdReport?.DigitalIdImage) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claim.AgencyReport.DigitalIdReport?.DigitalIdImageLongLat,
                LocationTime = claim.AgencyReport.DigitalIdReport?.DigitalIdImageLongLatTime,
                FacePercent = claim.AgencyReport.DigitalIdReport?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw ex;
        }
    }

    public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
    {
        try
        {
            var claim = claimsService.GetClaims().Include(c => c.AgencyReport).ThenInclude(c => c.PanIdReport).FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
            if (claim.AgencyReport == null)
            {
                claim.AgencyReport = new AgencyReport();
            }
            claim.AgencyReport.AgentEmail = data.Email;
            claim.AgencyReport.PanIdReport.DocumentIdImageLongLat = data.OcrLongLat;
            claim.AgencyReport.PanIdReport.DocumentIdImageLongLatTime = DateTime.Now;
            var longLat = claim.AgencyReport.PanIdReport.DocumentIdImageLongLat.IndexOf("/");
            var latitude = claim.AgencyReport.PanIdReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.AgencyReport.PanIdReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            claim.AgencyReport.PanIdReport.DocumentIdImageLocationUrl = url;
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);

            #region PAN IMAGE PROCESSING

            //=================GOOGLE VISION API =========================

            var byteimage = Convert.FromBase64String(data.OcrImage);

            //await CompareFaces.DetectSampleAsync(byteimage);

            var googleDetecTask = googleApi.DetectTextAsync(byteimage);

            var addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(googleDetecTask, addressTask);

            var imageReadOnly = await googleDetecTask;
            if (imageReadOnly != null && imageReadOnly.Count > 0)
            {
                var allPanText = imageReadOnly.FirstOrDefault().Description;
                var panTextPre = allPanText.IndexOf(panNumber2Find);
                var panNumber = allPanText.Substring(panTextPre + panNumber2Find.Length + 1, 10);


                var ocrImaged = googleHelper.MaskPanTextInImage(byteimage, imageReadOnly, panNumber2Find);
                var docyTypePan = allPanText.IndexOf(panNumber2Find) > 0 && allPanText.Length > allPanText.IndexOf(panNumber2Find) ? "PAN" : "UNKNOWN";
                var maskedImage = new FaceImageDetail
                {
                    DocType = docyTypePan,
                    DocumentId = panNumber,
                    MaskedImage = Convert.ToBase64String(ocrImaged),
                    OcrData = allPanText
                };
                try
                {
                    #region// PAN VERIFICATION ::: //test PAN FNLPM8635N, BYSPP5796F
                    if (company.VerifyPan)
                    {
                        //var body = await httpClientService.VerifyPan(maskedImage.DocumentId, company.PanIdfyUrl, company.RapidAPIKey, company.RapidAPITaskId, company.RapidAPIGroupId);
                        //company.RapidAPIPanRemainCount = body?.count_remain;

                        //if (body != null && body?.status == "completed" &&
                        //    body?.result != null &&
                        //    body.result?.source_output != null
                        //    && body.result?.source_output?.status == "id_found")
                        var panResponse = await httpClientService.VerifyPanNew(maskedImage.DocumentId, company.PanIdfyUrl, company.RapidAPIKey, company.RapidAPIHost);
                        if (panResponse != null && panResponse.valid)
                        {
                            var panMatch = panRegex.Match(maskedImage.DocumentId);
                            claim.AgencyReport.PanIdReport.DocumentIdImageValid = panMatch.Success && panResponse.valid ? true : false;
                        }
                    }
                    else
                    {
                        var panMatch = panRegex.Match(maskedImage.DocumentId);
                        claim.AgencyReport.PanIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
                    }

                    #endregion PAN IMAGE PROCESSING

                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    var savedMaskedImage = CompressImage.ProcessCompress(image);
                    claim.AgencyReport.PanIdReport.DocumentIdImage = savedMaskedImage;
                    claim.AgencyReport.PanIdReport.DocumentIdImageType = maskedImage.DocType;
                    claim.AgencyReport.PanIdReport.DocumentIdImageData = maskedImage.DocType + " data: ";

                    if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                    {
                        claim.AgencyReport.PanIdReport.DocumentIdImageData = maskedImage.DocType + " data:. \r\n " +
                            "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    claim.AgencyReport.PanIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
                    claim.AgencyReport.PanIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                    claim.AgencyReport.PanIdReport.DocumentIdImageData = "no data: ";
                }
            }
            //=================END GOOGLE VISION  API =========================

            else
            {
                var image = Convert.FromBase64String(data.OcrImage);
                claim.AgencyReport.PanIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
                claim.AgencyReport.PanIdReport.DocumentIdImageValid = false;
                claim.AgencyReport.PanIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                claim.AgencyReport.PanIdReport.DocumentIdImageData = "no data: ";
            }

            #endregion PAN IMAGE PROCESSING
            var rawAddress = await addressTask;
            claim.AgencyReport.PanIdReport.DocumentIdImageLocationAddress = rawAddress;
            claim.AgencyReport.PanIdReport.ValidationExecuted = true;

            _context.ClaimsInvestigation.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                OcrImage = claim.AgencyReport.PanIdReport?.DocumentIdImage != null ?
                Convert.ToBase64String(claim.AgencyReport.PanIdReport?.DocumentIdImage) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claim.AgencyReport.PanIdReport?.DocumentIdImageLongLat,
                OcrTime = claim.AgencyReport.PanIdReport?.DocumentIdImageLongLatTime,
                PanValid = claim.AgencyReport.PanIdReport?.DocumentIdImageValid
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public async Task<AppiCheckifyResponse> GetAudio(AudioData data)
    {
        try
        {

            var claim = claimsService.GetClaims()
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.AudioReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
            if (claim.AgencyReport == null)
            {
                claim.AgencyReport = new AgencyReport();
            }
            claim.AgencyReport.AgentEmail = data.Email;
            claim.AgencyReport.AudioReport.DocumentIdImageLongLat = data.LongLat;
            claim.AgencyReport.AudioReport.DocumentIdImageLongLatTime = DateTime.Now;
            var longLat = claim.AgencyReport.AudioReport.DocumentIdImageLongLat.IndexOf("/");
            var latitude = claim.AgencyReport.AudioReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.AgencyReport.AudioReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            claim.AgencyReport.AudioReport.DocumentIdImageLocationUrl = url;

            var rawAddressTask = httpClientService.GetRawAddress(latitude, longitude);
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherDataTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);

            

            claim.AgencyReport.AudioReport.ValidationExecuted = true;
            claim.AgencyReport.AudioReport.DocumentIdImageValid = true;

            claim.AgencyReport.AudioReport.DocumentIdImage = data.Mediabytes;

            //TO-DO: AWS : SPEECH TO TEXT;
            string audioDirectory = Path.Combine(webHostEnvironment.WebRootPath, "audio");
            if (!Directory.Exists(audioDirectory))
            {
                Directory.CreateDirectory(audioDirectory);
            }
            string audioFileName = "audio"+ DateTime.Now.ToString("ddMMMyyyy") + data.Name;
            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "audio", audioFileName);
            var audioFileTask = File.WriteAllBytesAsync(filePath, data.Mediabytes);

            var audio2Texttask = httpClientService.TranscribeAsync(audioS3FolderName, audioFileName, filePath);

            await Task.WhenAll(rawAddressTask, weatherDataTask, audioFileTask, audio2Texttask);
            var rawAddress = await rawAddressTask;
            claim.AgencyReport.AudioReport.DocumentIdImageLocationAddress = rawAddress;
            await audioFileTask;
            var weatherData = await weatherDataTask;
            await audio2Texttask;
            var audioResult = await audio2Texttask;
            string audioData = "No data";
            if (weatherData != null && weatherData.current != null && weatherData.current.temperature_2m != null)
            {
                audioData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";
            }
            if (audioResult != null && audioResult.results != null && audioResult.results.audio_segments != null && audioResult.results.audio_segments.Count > 0)
            {
                audioData = audioData +  $"\r\n" +
                    $"\r\n" +
                $"\r\nAudio Text:  :{string.Join(" ", audioResult.results.audio_segments.Select(s => s.transcript))}";
            }

            claim.AgencyReport.AudioReport.DocumentIdImageData = audioData;
            claim.AgencyReport.AudioReport.DocumentIdImagePath = filePath;
            //END :: TO-DO: AWS : SPEECH TO TEXT;

            var updatedClaim = _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updatedClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                OcrImage = claim.AgencyReport.AudioReport?.DocumentIdImage != null ?
                Convert.ToBase64String(claim.AgencyReport.AudioReport?.DocumentIdImage) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claim.AgencyReport.AudioReport?.DocumentIdImageLongLat,
                OcrTime = claim.AgencyReport.AudioReport?.DocumentIdImageLongLatTime,
                PanValid = claim.AgencyReport.AudioReport?.DocumentIdImageValid
            };

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public async Task<AppiCheckifyResponse> GetVideo(VideoData data)
    {
        try
        {

            var claim = claimsService.GetClaims()
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.VideoReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
            if (claim.AgencyReport == null)
            {
                claim.AgencyReport = new AgencyReport();
            }
            claim.AgencyReport.AgentEmail = data.Email;
            claim.AgencyReport.VideoReport.DocumentIdImageLongLat = data.LongLat;
            claim.AgencyReport.VideoReport.DocumentIdImageLongLatTime = DateTime.Now;
            var longLat = claim.AgencyReport.VideoReport.DocumentIdImageLongLat.IndexOf("/");
            var latitude = claim.AgencyReport.VideoReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.AgencyReport.VideoReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            claim.AgencyReport.VideoReport.DocumentIdImageLocationUrl = url;

            var rawAddress = await httpClientService.GetRawAddress(latitude, longitude);
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";

            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";

            claim.AgencyReport.VideoReport.DocumentIdImageData = weatherCustomData;
            claim.AgencyReport.VideoReport.DocumentIdImageLocationAddress = rawAddress;

            claim.AgencyReport.VideoReport.ValidationExecuted = true;
            claim.AgencyReport.VideoReport.DocumentIdImageValid = true;
            claim.AgencyReport.VideoReport.DocumentIdImage = data.Mediabytes;

            //TO-DO: AWS : SPEECH TO TEXT;

            string videooDirectory = Path.Combine(webHostEnvironment.WebRootPath, "video");
            if (!Directory.Exists(videooDirectory))
            {
                Directory.CreateDirectory(videooDirectory);
            }
            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "video", data.Email + DateTime.Now.ToString("ddMMMyyyy") + data.Name);
            await File.WriteAllBytesAsync(filePath, data.Mediabytes);
            claim.AgencyReport.VideoReport.DocumentIdImagePath = filePath;
            //END :: TO-DO: AWS : SPEECH TO TEXT;


            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                OcrImage = claim.AgencyReport.VideoReport?.DocumentIdImage != null ?
                Convert.ToBase64String(claim.AgencyReport.VideoReport?.DocumentIdImage) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claim.AgencyReport.VideoReport?.DocumentIdImageLongLat,
                OcrTime = claim.AgencyReport.VideoReport?.DocumentIdImageLongLatTime,
                PanValid = claim.AgencyReport.VideoReport?.DocumentIdImageValid
            };

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public async Task<AppiCheckifyResponse> GetPassportId(DocumentData data)
    {
        try
        {
            var claim = claimsService.GetClaims().Include(c => c.AgencyReport).ThenInclude(c => c.PassportIdReport).FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
            if (claim.AgencyReport == null)
            {
                claim.AgencyReport = new AgencyReport();
            }
            claim.AgencyReport.AgentEmail = data.Email;
            claim.AgencyReport.PassportIdReport.DocumentIdImageLongLat = data.OcrLongLat;
            claim.AgencyReport.PassportIdReport.DocumentIdImageLongLatTime = DateTime.Now;
            var longLat = claim.AgencyReport.PassportIdReport.DocumentIdImageLongLat.IndexOf("/");
            var latitude = claim.AgencyReport.PassportIdReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.AgencyReport.PassportIdReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            claim.AgencyReport.PassportIdReport.DocumentIdImageLocationUrl = url;
            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);

            #region Passport IMAGE PROCESSING

            //=================GOOGLE VISION API =========================

            var byteimage = Convert.FromBase64String(data.OcrImage);

            var passportdata = await httpClientService.GetPassportOcrResult(byteimage, company.PassportApiUrl, company.PassportApiKey, company.PassportApiHost);

            var googleDetecTask = googleApi.DetectTextAsync(byteimage);

            var addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(googleDetecTask, addressTask);

            var imageReadOnly = await googleDetecTask;
            if (imageReadOnly != null && imageReadOnly.Count > 0)
            {
                var allPassportText = imageReadOnly.FirstOrDefault().Description;
                var passportTextPre = allPassportText.IndexOf(passportNumber2Find);
                var dateOfBirth2FindTextPre = allPassportText.IndexOf(dateOfBirth2Find);

                var passportNumber = allPassportText.Substring(passportTextPre + passportNumber2Find.Length + 1, 8);
                var dateOfBirthNumber = allPassportText.Substring(dateOfBirth2FindTextPre + dateOfBirth2Find.Length + 1, 8);

                var passportMatch = passportRegex.Match(passportNumber);
                if (!passportMatch.Success)
                {
                    passportNumber = passportRegex.Match(allPassportText).Value;
                }
                var dateOfBirth = dateOfBirthRegex.Match(allPassportText).Value;


                var ocrImaged = googleHelper.MaskPassportTextInImage(byteimage, imageReadOnly, passportNumber);
                var docyTypePassport = allPassportText.IndexOf(passportNumber2Find) > 0 && allPassportText.Length > allPassportText.IndexOf(passportNumber2Find) ? "Passport" : "UNKNOWN";
                var maskedImage = new FaceImageDetail
                {
                    DocType = docyTypePassport,
                    DocumentId = passportdata != null ? passportdata.data.ocr.documentNumber : passportNumber,
                    MaskedImage = Convert.ToBase64String(ocrImaged),
                    OcrData = allPassportText,
                    DateOfBirth = passportdata != null ? passportdata.data.ocr.dateOfBirth : dateOfBirth
                };
                try
                {
                    #region// PASSPORT VERIFICATION ::: //test 
                    if (company.VerifyPassport)
                    {
                        //to-do
                        var passportResponse = await httpClientService.VerifyPassport(maskedImage.DocumentId, maskedImage.DateOfBirth);
                        var panMatch = passportRegex.Match(maskedImage.DocumentId);
                        claim.AgencyReport.PassportIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
                    }
                    else
                    {
                        var panMatch = passportRegex.Match(maskedImage.DocumentId);
                        claim.AgencyReport.PassportIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
                    }

                    #endregion PAN IMAGE PROCESSING

                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    var savedMaskedImage = CompressImage.ProcessCompress(image);
                    claim.AgencyReport.PassportIdReport.DocumentIdImage = savedMaskedImage;
                    claim.AgencyReport.PassportIdReport.DocumentIdImageType = maskedImage.DocType;
                    claim.AgencyReport.PassportIdReport.DocumentIdImageData = maskedImage.DocType + " data: ";

                    if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                    {
                        claim.AgencyReport.PassportIdReport.DocumentIdImageData = maskedImage.DocType + " data:. \r\n " +
                            "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    claim.AgencyReport.PassportIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
                    claim.AgencyReport.PassportIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                    claim.AgencyReport.PassportIdReport.DocumentIdImageData = "no data: ";
                }
            }
            //=================END GOOGLE VISION  API =========================

            else
            {
                var image = Convert.FromBase64String(data.OcrImage);
                claim.AgencyReport.PassportIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
                claim.AgencyReport.PassportIdReport.DocumentIdImageValid = false;
                claim.AgencyReport.PassportIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                claim.AgencyReport.PassportIdReport.DocumentIdImageData = "no data: ";
            }

            #endregion PAN IMAGE PROCESSING
            var rawAddress = await addressTask;
            claim.AgencyReport.PassportIdReport.DocumentIdImageLocationAddress = rawAddress;
            claim.AgencyReport.PassportIdReport.ValidationExecuted = true;

            _context.ClaimsInvestigation.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                OcrImage = claim.AgencyReport.PassportIdReport?.DocumentIdImage != null ?
                Convert.ToBase64String(claim.AgencyReport.PassportIdReport?.DocumentIdImage) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claim.AgencyReport.PassportIdReport?.DocumentIdImageLongLat,
                OcrTime = claim.AgencyReport.PassportIdReport?.DocumentIdImageLongLatTime,
                PanValid = claim.AgencyReport.PassportIdReport?.DocumentIdImageValid
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
}