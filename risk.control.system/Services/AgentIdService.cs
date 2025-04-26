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
using Google.Api;
using static risk.control.system.Helpers.Permissions;
using System.Security.Claims;

namespace risk.control.system.Services;

public interface IAgentIdService
{
    Task<AppiCheckifyResponse> GetAgentId(FaceData data);
    Task<AppiCheckifyResponse> GetFaceId(FaceData data);
    Task<AppiCheckifyResponse> GetDocumentId(DocumentData data);

}

public class AgentIdService : IAgentIdService
{
    private static string panNumber2Find = "Permanent Account Number";
    private static Regex panRegex = new Regex(@"[A-Z]{5}\d{4}[A-Z]{1}");
    private readonly ApplicationDbContext _context;
    private readonly IGoogleApi googleApi;
    private readonly IGoogleMaskHelper googleHelper;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiCLient customApiCLient;
    private readonly IClaimsService claimsService;
    private readonly IFaceMatchService faceMatchService;
    private readonly IWebHostEnvironment webHostEnvironment;

    private static HttpClient httpClient = new();

    //test PAN FNLPM8635N
    public AgentIdService(ApplicationDbContext context, IGoogleApi googleApi,
        IGoogleMaskHelper googleHelper, 
        IHttpClientService httpClientService,
        ICustomApiCLient customApiCLient,
        IClaimsService claimsService,
        IFaceMatchService faceMatchService,
        IWebHostEnvironment webHostEnvironment)
    {
        this._context = context;
        this.googleApi = googleApi;
        this.googleHelper = googleHelper;
        this.httpClientService = httpClientService;
        this.customApiCLient = customApiCLient;
        this.claimsService = claimsService;
        this.faceMatchService = faceMatchService;
        this.webHostEnvironment = webHostEnvironment;
    }
    
    public async Task<AppiCheckifyResponse> GetAgentId(FaceData data)
    {
        InvestigationTask claim = null;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .FirstOrDefaultAsync(c => c.Id == data.ClaimId);

            if (claim.InvestigationReport == null)
            {
                claim.InvestigationReport = new InvestigationReport();
            }
            
            claim.InvestigationReport.AgentEmail = data.Email;
            var agent = _context.VendorApplicationUser.FirstOrDefault(u=>u.Email == data.Email);
            claim.InvestigationReport.AgentIdReport.Updated = DateTime.Now;
            claim.InvestigationReport.AgentIdReport.UpdatedBy = data.Email;
            claim.InvestigationReport.AgentIdReport.IdImageLongLatTime = DateTime.Now;
            claim.InvestigationReport.AgentIdReport.IdImageLongLat = data.LocationLongLat;
            var longLat = claim.InvestigationReport.AgentIdReport.IdImageLongLat.IndexOf("/");
            var latitude = claim.InvestigationReport.AgentIdReport.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.InvestigationReport.AgentIdReport.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";

            byte[]? registeredImage = agent.ProfilePicture;

            var expectedLat = string.Empty;
            var expectedLong = string.Empty;
            if (claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM)
            {
                expectedLat = claim.BeneficiaryDetail.Latitude;
                expectedLong = claim.BeneficiaryDetail.Longitude;
            }
            else
            {

                expectedLat = claim.CustomerDetail.Latitude;
                expectedLong = claim.CustomerDetail.Longitude;
            }

            var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");

            #region FACE IMAGE PROCESSING

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, data.LocationImage);
            var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            var addressTask = httpClientService.GetRawAddress(latitude, longitude);
            #endregion FACE IMAGE PROCESSING

            await Task.WhenAll(faceMatchTask, addressTask, weatherTask, mapTask);

            var (confidence, compressImage, similarity) = await faceMatchTask;
            var address = await addressTask;
            var weatherData = await weatherTask;
            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;


            claim.InvestigationReport.AgentIdReport.IdImageLocationUrl = map;
            claim.InvestigationReport.AgentIdReport.Duration = duration;
            claim.InvestigationReport.AgentIdReport.Distance = distance;
            claim.InvestigationReport.AgentIdReport.DistanceInMetres = distanceInMetres;
            claim.InvestigationReport.AgentIdReport.DurationInSeconds = durationInSecs;


            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";

            claim.InvestigationReport.AgentIdReport.IdImageData = weatherCustomData;
            claim.InvestigationReport.AgentIdReport.IdImage = compressImage;
            claim.InvestigationReport.AgentIdReport.DigitalIdImageMatchConfidence = confidence;
            claim.InvestigationReport.AgentIdReport.IdImageLocationAddress = address;
            claim.InvestigationReport.AgentIdReport.ValidationExecuted = true;
            claim.InvestigationReport.AgentIdReport.Similarity = similarity;
            var updateClaim = _context.Investigations.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = updateClaim.Entity.InvestigationReport?.AgentIdReport?.IdImage != null ?
                Convert.ToBase64String(claim.InvestigationReport?.AgentIdReport?.IdImage) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claim.InvestigationReport.AgentIdReport?.IdImageLongLat,
                LocationTime = claim.InvestigationReport.AgentIdReport?.IdImageLongLatTime,
                FacePercent = claim.InvestigationReport.AgentIdReport?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            claim.InvestigationReport.AgentIdReport.IdImageData = "No Weather Data";
            claim.InvestigationReport.AgentIdReport.IdImage = Convert.FromBase64String(data.LocationImage);
            claim.InvestigationReport.AgentIdReport.DigitalIdImageMatchConfidence = string.Empty;
            claim.InvestigationReport.AgentIdReport.IdImageLocationAddress = "No Address data";
            claim.InvestigationReport.AgentIdReport.ValidationExecuted = true;
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var noData = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = updateClaim.Entity.InvestigationReport?.AgentIdReport?.IdImage != null ?
                Convert.ToBase64String(claim.InvestigationReport?.AgentIdReport?.IdImage) :
                Convert.ToBase64String(noData),
                LocationLongLat = claim.InvestigationReport.AgentIdReport?.IdImageLongLat,
                LocationTime = claim.InvestigationReport.AgentIdReport?.IdImageLongLatTime,
                FacePercent = claim.InvestigationReport.AgentIdReport?.DigitalIdImageMatchConfidence
            };
        }
    }

    public async Task<AppiCheckifyResponse> GetFaceId(FaceData data)
    {
        InvestigationTask claim = null;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .FirstOrDefaultAsync(c => c.Id == data.ClaimId);
            claim.InvestigationReport.AgentEmail = data.Email;
            claim.InvestigationReport.DigitalIdReport.Updated = DateTime.Now;
            claim.InvestigationReport.DigitalIdReport.UpdatedBy = data.Email;
            claim.InvestigationReport.DigitalIdReport.IdImageLongLatTime = DateTime.Now;
            claim.InvestigationReport.DigitalIdReport.IdImageLongLat = data.LocationLongLat;
            var longLat = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.IndexOf("/");
            var latitude = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.InvestigationReport.DigitalIdReport.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";

            var expectedLat = string.Empty;
            var expectedLong = string.Empty;
            byte[]? registeredImage = null;
            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                registeredImage = claim.CustomerDetail.ProfilePicture;
                expectedLat = claim.CustomerDetail.Latitude;
                expectedLong = claim.CustomerDetail.Longitude;
            }
            else
            {
                registeredImage = claim.BeneficiaryDetail.ProfilePicture;
                expectedLat = claim.BeneficiaryDetail.Latitude;
                expectedLong = claim.BeneficiaryDetail.Longitude;
            }

            var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");

            #region FACE IMAGE PROCESSING

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, data.LocationImage);
            var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            var addressTask = httpClientService.GetRawAddress(latitude, longitude);
            #endregion FACE IMAGE PROCESSING

            await Task.WhenAll(faceMatchTask, addressTask, weatherTask, mapTask);

            var (confidence, compressImage, similarity) = await faceMatchTask;
            var address = await addressTask;
            var weatherData = await weatherTask;
            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;


            claim.InvestigationReport.DigitalIdReport.IdImageLocationUrl = map;
            claim.InvestigationReport.DigitalIdReport.Duration = duration;
            claim.InvestigationReport.DigitalIdReport.Distance = distance;
            claim.InvestigationReport.DigitalIdReport.DistanceInMetres = distanceInMetres;
            claim.InvestigationReport.DigitalIdReport.DurationInSeconds = durationInSecs;


            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";

            claim.InvestigationReport.DigitalIdReport.IdImageData = weatherCustomData;
            claim.InvestigationReport.DigitalIdReport.IdImage = compressImage;
            claim.InvestigationReport.DigitalIdReport.DigitalIdImageMatchConfidence = confidence;
            claim.InvestigationReport.DigitalIdReport.IdImageLocationAddress = address;
            claim.InvestigationReport.DigitalIdReport.ValidationExecuted = true;
            claim.InvestigationReport.DigitalIdReport.Similarity = similarity;
            var updateClaim = _context.Investigations.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = updateClaim.Entity.InvestigationReport?.DigitalIdReport?.IdImage != null ?
                Convert.ToBase64String(claim.InvestigationReport?.DigitalIdReport?.IdImage) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claim.InvestigationReport.DigitalIdReport?.IdImageLongLat,
                LocationTime = claim.InvestigationReport.DigitalIdReport?.IdImageLongLatTime,
                FacePercent = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            claim.InvestigationReport.DigitalIdReport.IdImageData = "No Weather Data";
            claim.InvestigationReport.DigitalIdReport.IdImage = Convert.FromBase64String(data.LocationImage);
            claim.InvestigationReport.DigitalIdReport.DigitalIdImageMatchConfidence = string.Empty;
            claim.InvestigationReport.DigitalIdReport.IdImageLocationAddress = "No Address data";
            claim.InvestigationReport.DigitalIdReport.ValidationExecuted = true;
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var noData = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = updateClaim.Entity.InvestigationReport?.DigitalIdReport?.IdImage != null ?
                Convert.ToBase64String(claim.InvestigationReport?.DigitalIdReport?.IdImage) :
                Convert.ToBase64String(noData),
                LocationLongLat = claim.InvestigationReport.DigitalIdReport?.IdImageLongLat,
                LocationTime = claim.InvestigationReport.DigitalIdReport?.IdImageLongLatTime,
                FacePercent = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageMatchConfidence
            };

        }
    }

    public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
    {
        InvestigationTask claim = null;
        Task<string> addressTask = null;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .FirstOrDefaultAsync(c => c.Id == data.ClaimId);

            claim.InvestigationReport.AgentEmail = data.Email;
            claim.InvestigationReport.PanIdReport.IdImageLongLat = data.OcrLongLat;
            claim.InvestigationReport.PanIdReport.IdImageLongLatTime = DateTime.Now;
            var longLat = claim.InvestigationReport.PanIdReport.IdImageLongLat.IndexOf("/");
            var latitude = claim.InvestigationReport.PanIdReport.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = claim.InvestigationReport.PanIdReport.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            var expectedLat = string.Empty;
            var expectedLong = string.Empty;
            if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                expectedLat = claim.CustomerDetail.Latitude;
                expectedLong = claim.CustomerDetail.Longitude;
            }
            else
            {
                expectedLat = claim.BeneficiaryDetail.Latitude;
                expectedLong = claim.BeneficiaryDetail.Longitude;
            }
            var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");

            #region PAN IMAGE PROCESSING

            //=================GOOGLE VISION API =========================

            var byteimage = Convert.FromBase64String(data.OcrImage);

            //await CompareFaces.DetectSampleAsync(byteimage);

            var googleDetecTask = googleApi.DetectTextAsync(byteimage);

            addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(googleDetecTask, addressTask, mapTask);

            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;
            claim.InvestigationReport.PanIdReport.DistanceInMetres = distanceInMetres;
            claim.InvestigationReport.PanIdReport.DurationInSeconds = durationInSecs;
            claim.InvestigationReport.PanIdReport.Duration = duration;
            claim.InvestigationReport.PanIdReport.Distance = distance;
            claim.InvestigationReport.PanIdReport.IdImageLocationUrl = map;

            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);
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
                        var panResponse = await httpClientService.VerifyPanNew(maskedImage.DocumentId, company.PanIdfyUrl, company.PanAPIKey, company.PanAPIHost);
                        if (panResponse != null && panResponse.valid)
                        {
                            var panMatch = panRegex.Match(maskedImage.DocumentId);
                            claim.InvestigationReport.PanIdReport.DocumentIdImageValid = panMatch.Success && panResponse.valid ? true : false;
                        }
                    }
                    else
                    {
                        var panMatch = panRegex.Match(maskedImage.DocumentId);
                        claim.InvestigationReport.PanIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
                    }

                    #endregion PAN IMAGE PROCESSING

                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    var savedMaskedImage = CompressImage.ProcessCompress(image);
                    claim.InvestigationReport.PanIdReport.IdImage = savedMaskedImage;
                    claim.InvestigationReport.PanIdReport.DocumentIdImageType = maskedImage.DocType;
                    claim.InvestigationReport.PanIdReport.IdImageData = maskedImage.DocType + " data: ";

                    if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                    {
                        claim.InvestigationReport.PanIdReport.IdImageData = maskedImage.DocType + " data:. \r\n " +
                            "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    claim.InvestigationReport.PanIdReport.IdImage = CompressImage.ProcessCompress(image);
                    claim.InvestigationReport.PanIdReport.IdImageLongLatTime = DateTime.Now;
                    claim.InvestigationReport.PanIdReport.IdImageData = "no data: ";
                }
            }
            //=================END GOOGLE VISION  API =========================

            else
            {
                var image = Convert.FromBase64String(data.OcrImage);
                claim.InvestigationReport.PanIdReport.IdImage = CompressImage.ProcessCompress(image);
                claim.InvestigationReport.PanIdReport.DocumentIdImageValid = false;
                claim.InvestigationReport.PanIdReport.IdImageLongLatTime = DateTime.Now;
                claim.InvestigationReport.PanIdReport.IdImageData = "no data: ";
            }

            #endregion PAN IMAGE PROCESSING
            var rawAddress = await addressTask;
            claim.InvestigationReport.PanIdReport.IdImageLocationAddress = rawAddress;
            claim.InvestigationReport.PanIdReport.ValidationExecuted = true;

            _context.Investigations.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                OcrImage = claim.InvestigationReport.PanIdReport?.IdImage != null ?
                Convert.ToBase64String(claim.InvestigationReport.PanIdReport?.IdImage) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claim.InvestigationReport.PanIdReport?.IdImageLongLat,
                OcrTime = claim.InvestigationReport.PanIdReport?.IdImageLongLatTime,
                PanValid = claim.InvestigationReport.PanIdReport?.DocumentIdImageValid
            };
        }
        catch (Exception)
        {

            throw;
        }
    }
}