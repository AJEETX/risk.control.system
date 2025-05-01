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
        DigitalIdReport face = null;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
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
                .FirstOrDefaultAsync(c => c.Id == data.ClaimId);

            if (claim.InvestigationReport == null)
            {
                return null;
            }
            face = await _context.DigitalIdReport.FindAsync(data.FaceId);

            var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == data.Email);
            if(data.IsAgent)
            {
                var location = _context.LocationTemplate.FirstOrDefault(l => l.Id == data.LocationId);
                location.AgentEmail = agent.Email;
                _context.LocationTemplate.Update(location);
            }
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.IdImageLongLatTime = DateTime.Now;
            face.IdImageLongLat = data.LocationLongLat;
            var longLat = face.IdImageLongLat.IndexOf("/");
            var latitude = face.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = face.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            byte[]? registeredImage = null;
            if (data.IsAgent)
            {
                registeredImage = agent.ProfilePicture;
            }
            else
            {
                if (claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM)
                {
                    registeredImage = claim.BeneficiaryDetail.ProfilePicture;
                }
                else
                {
                    registeredImage = claim.CustomerDetail.ProfilePicture;
                }
            }

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


            face.IdImageLocationUrl = map;
            face.Duration = duration;
            face.Distance = distance;
            face.DistanceInMetres = distanceInMetres;
            face.DurationInSeconds = durationInSecs;


            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";

            face.IdImageData = weatherCustomData;
            face.IdImage = compressImage;
            face.DigitalIdImageMatchConfidence = confidence;
            face.IdImageLocationAddress = address;
            face.ValidationExecuted = true;
            face.Similarity = similarity;
            _context.DigitalIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = face?.IdImage != null ?
                Convert.ToBase64String(face.IdImage) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = face.IdImageLongLat,
                LocationTime = face?.IdImageLongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            face.IdImageData = "No Weather Data";
            face.IdImage = Convert.FromBase64String(data.LocationImage);
            face.DigitalIdImageMatchConfidence = string.Empty;
            face.IdImageLocationAddress = "No Address data";
            face.ValidationExecuted = true;
            _context.DigitalIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var noData = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = face?.IdImage != null ?
                Convert.ToBase64String(face?.IdImage) :
                Convert.ToBase64String(noData),
                LocationLongLat = face?.IdImageLongLat,
                LocationTime = face?.IdImageLongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
    }

    public async Task<AppiCheckifyResponse> GetFaceId(FaceData data)
    {
        InvestigationTask claim = null;
        DigitalIdReport face = null;
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
        DocumentIdReport documentIdReport = null;
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


            documentIdReport = _context.DocumentIdReport.FirstOrDefault(c => c.Id == data.DocId);

            documentIdReport.IdImageLongLat = data.OcrLongLat;
            documentIdReport.IdImageLongLatTime = DateTime.Now;
            var longLat = documentIdReport.IdImageLongLat.IndexOf("/");
            var latitude = documentIdReport.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = documentIdReport.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
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
            documentIdReport.DistanceInMetres = distanceInMetres;
            documentIdReport.DurationInSeconds = durationInSecs;
            documentIdReport.Duration = duration;
            documentIdReport.Distance = distance;
            documentIdReport.IdImageLocationUrl = map;

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
                            documentIdReport.DocumentIdImageValid = panMatch.Success && panResponse.valid ? true : false;
                        }
                    }
                    else
                    {
                        var panMatch = panRegex.Match(maskedImage.DocumentId);
                        documentIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
                    }

                    #endregion PAN IMAGE PROCESSING

                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    var savedMaskedImage = CompressImage.ProcessCompress(image);
                    documentIdReport.IdImage = savedMaskedImage;
                    documentIdReport.IdImageData = maskedImage.DocType + " data: ";

                    if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                    {
                        documentIdReport.IdImageData = maskedImage.DocType + " data:. \r\n " +
                            "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    documentIdReport.IdImage = CompressImage.ProcessCompress(image);
                    documentIdReport.IdImageLongLatTime = DateTime.Now;
                    documentIdReport.IdImageData = "no data: ";
                }
            }
            //=================END GOOGLE VISION  API =========================

            else
            {
                var image = Convert.FromBase64String(data.OcrImage);
                documentIdReport.IdImage = CompressImage.ProcessCompress(image);
                documentIdReport.DocumentIdImageValid = false;
                documentIdReport.IdImageLongLatTime = DateTime.Now;
                documentIdReport.IdImageData = "no data: ";
            }

            #endregion PAN IMAGE PROCESSING
            var rawAddress = await addressTask;
            documentIdReport.IdImageLocationAddress = rawAddress;
            documentIdReport.ValidationExecuted = true;

            _context.Investigations.Update(claim);

            var rows = await _context.SaveChangesAsync();

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                OcrImage = documentIdReport?.IdImage != null ?
                Convert.ToBase64String(documentIdReport?.IdImage) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = documentIdReport?.IdImageLongLat,
                OcrTime = documentIdReport?.IdImageLongLatTime,
                Valid = documentIdReport?.DocumentIdImageValid
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            documentIdReport.IdImageData = "No Weather Data";
            documentIdReport.IdImage = Convert.FromBase64String(data.OcrImage);
            documentIdReport.DocumentIdImageValid = false;
            documentIdReport.IdImageLocationAddress = "No Address data";
            documentIdReport.ValidationExecuted = true;
            _context.DocumentIdReport.Update(documentIdReport);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
            var noData = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                LocationImage = documentIdReport?.IdImage != null ?
                Convert.ToBase64String(documentIdReport?.IdImage) :
                Convert.ToBase64String(noData),
                LocationLongLat = documentIdReport?.IdImageLongLat,
                LocationTime = documentIdReport?.IdImageLongLatTime,
                Valid = documentIdReport?.DocumentIdImageValid
            };
        }
    }
}