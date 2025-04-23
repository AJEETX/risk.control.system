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

namespace risk.control.system.Services;

public interface IICheckifyService
{
    //Task<AppiCheckifyResponse> GetAgentId(FaceData data);
    //Task<AppiCheckifyResponse> GetFaceId(FaceData data);
    //Task<AppiCheckifyResponse> GetDocumentId(DocumentData data);
    //Task<AppiCheckifyResponse> GetPassportId(DocumentData data);
    //Task<AppiCheckifyResponse> GetAudio(AudioData data);
    //Task<AppiCheckifyResponse> GetVideo(VideoData data);
    Task<bool> WhitelistIP(IPWhitelistRequest request);
}

public class ICheckifyService : IICheckifyService
{
    private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
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
    private readonly ICustomApiCLient customApiCLient;
    private readonly IClaimsService claimsService;
    private readonly IFaceMatchService faceMatchService;
    private readonly IWebHostEnvironment webHostEnvironment;

    private static HttpClient httpClient = new();

    //test PAN FNLPM8635N
    public ICheckifyService(ApplicationDbContext context, IGoogleApi googleApi,
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

    //public async Task<AppiCheckifyResponse> GetAgentId(FaceData data)
    //{
    //    ClaimsInvestigation claim = null;
    //    try
    //    {
    //        claim = claimsService.GetClaims().Include(c => c.InvestigationReport).ThenInclude(c => c.AgentIdReport).FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

    //        if (claim.InvestigationReport == null)
    //        {
    //            claim.InvestigationReport = new InvestigationReport();
    //        }
            
    //        var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;
    //        if (isClaim)
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date and time of death ?";
    //        }
    //        else
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
    //        }

    //        claim.InvestigationReport.AgentEmail = data.Email;
    //        var agent = _context.VendorApplicationUser.FirstOrDefault(u=>u.Email == data.Email);
    //        claim.InvestigationReport.AgentIdReport.Updated = DateTime.Now;
    //        claim.InvestigationReport.AgentIdReport.UpdatedBy = data.Email;
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLatTime = DateTime.Now;
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLat = data.LocationLongLat;
    //        var longLat = claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLat.IndexOf("/");
    //        var latitude = claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
    //        var longitude = claim.InvestigationReport.AgentIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
    //        var latLongString = latitude + "," + longitude;
    //        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";

    //        byte[]? registeredImage = agent.ProfilePicture;

    //        var expectedLat = string.Empty;
    //        var expectedLong = string.Empty;
    //        if (claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM)
    //        {
    //            expectedLat = claim.BeneficiaryDetail.Latitude;
    //            expectedLong = claim.BeneficiaryDetail.Longitude;
    //        }
    //        else
    //        {

    //            expectedLat = claim.CustomerDetail.Latitude;
    //            expectedLong = claim.CustomerDetail.Longitude;
    //        }

    //        var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");

    //        #region FACE IMAGE PROCESSING

    //        var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, data.LocationImage);
    //        var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
    //        var addressTask = httpClientService.GetRawAddress(latitude, longitude);
    //        #endregion FACE IMAGE PROCESSING

    //        await Task.WhenAll(faceMatchTask, addressTask, weatherTask, mapTask);

    //        var (confidence, compressImage, similarity) = await faceMatchTask;
    //        var address = await addressTask;
    //        var weatherData = await weatherTask;
    //        var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;


    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageLocationUrl = map;
    //        claim.InvestigationReport.AgentIdReport.Duration = duration;
    //        claim.InvestigationReport.AgentIdReport.Distance = distance;
    //        claim.InvestigationReport.AgentIdReport.DistanceInMetres = distanceInMetres;
    //        claim.InvestigationReport.AgentIdReport.DurationInSeconds = durationInSecs;


    //        string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
    //            $"\r\n" +
    //            $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
    //            $"\r\n" +
    //            $"\r\nElevation(sea level):{weatherData.elevation} metres";

    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageData = weatherCustomData;
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImage = compressImage;
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageMatchConfidence = confidence;
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageLocationAddress = address;
    //        claim.InvestigationReport.AgentIdReport.MatchExecuted = true;
    //        claim.InvestigationReport.AgentIdReport.Similarity = similarity;
    //        var updateClaim = _context.ClaimsInvestigation.Update(claim);

    //        var rows = await _context.SaveChangesAsync();

    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

    //        var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
    //            LocationImage = updateClaim.Entity.InvestigationReport?.AgentIdReport?.DigitalIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport?.AgentIdReport?.DigitalIdImage) :
    //            Convert.ToBase64String(noDataimage),
    //            LocationLongLat = claim.InvestigationReport.AgentIdReport?.DigitalIdImageLongLat,
    //            LocationTime = claim.InvestigationReport.AgentIdReport?.DigitalIdImageLongLatTime,
    //            FacePercent = claim.InvestigationReport.AgentIdReport?.DigitalIdImageMatchConfidence
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.StackTrace);
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageData = "No Weather Data";
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImage = Convert.FromBase64String(data.LocationImage);
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageMatchConfidence = string.Empty;
    //        claim.InvestigationReport.AgentIdReport.DigitalIdImageLocationAddress = "No Address data";
    //        claim.InvestigationReport.AgentIdReport.MatchExecuted = true;
    //        var updateClaim = _context.ClaimsInvestigation.Update(claim);
    //        var rows = await _context.SaveChangesAsync();
    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
    //        var noData = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
    //            LocationImage = updateClaim.Entity.InvestigationReport?.AgentIdReport?.DigitalIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport?.AgentIdReport?.DigitalIdImage) :
    //            Convert.ToBase64String(noData),
    //            LocationLongLat = claim.InvestigationReport.AgentIdReport?.DigitalIdImageLongLat,
    //            LocationTime = claim.InvestigationReport.AgentIdReport?.DigitalIdImageLongLatTime,
    //            FacePercent = claim.InvestigationReport.AgentIdReport?.DigitalIdImageMatchConfidence
    //        };
    //    }
    //}
    //public async Task<AppiCheckifyResponse> GetFaceId(FaceData data)
    //{
    //    ClaimsInvestigation claim = null;
    //    try
    //    {
    //        claim = claimsService.GetClaims().Include(c => c.InvestigationReport).ThenInclude(c => c.DigitalIdReport).FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

    //        if (claim.InvestigationReport == null)
    //        {
    //            claim.InvestigationReport = new InvestigationReport();
    //        }
           
    //        var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;
    //        if (isClaim)
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date and time of death ?";
    //        }
    //        else
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
    //        }

    //        claim.InvestigationReport.AgentEmail = data.Email;
    //        claim.InvestigationReport.DigitalIdReport.Updated = DateTime.Now;
    //        claim.InvestigationReport.DigitalIdReport.UpdatedBy = data.Email;
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLatTime = DateTime.Now;
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat = data.LocationLongLat;
    //        var longLat = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
    //        var latitude = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
    //        var longitude = claim.InvestigationReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
    //        var latLongString = latitude + "," + longitude;
    //        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
    //        var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

    //        var expectedLat = string.Empty;
    //        var expectedLong = string.Empty;
    //        byte[]? registeredImage = null;
    //        if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
    //        {
    //            registeredImage = claim.CustomerDetail.ProfilePicture;
    //            expectedLat = claim.CustomerDetail.Latitude ;
    //            expectedLong =  claim.CustomerDetail.Longitude;
    //        }
    //        else
    //        {
    //            registeredImage = claim.BeneficiaryDetail.ProfilePicture;
    //            expectedLat = claim.BeneficiaryDetail.Latitude;
    //            expectedLong = claim.BeneficiaryDetail.Longitude;
    //        }

    //        var mapTask = customApiCLient.GetMap(double.Parse(expectedLat),double.Parse(expectedLong),double.Parse(latitude),double.Parse(longitude),"A","X","300","300","green","red");

    //        #region FACE IMAGE PROCESSING

    //        var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, data.LocationImage);
    //        var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
    //        var addressTask = httpClientService.GetRawAddress(latitude, longitude);
    //        #endregion FACE IMAGE PROCESSING

    //        await Task.WhenAll(faceMatchTask, addressTask, weatherTask, mapTask);

    //        var (confidence, compressImage, similarity) = await faceMatchTask;
    //        var address = await addressTask;
    //        var weatherData = await weatherTask;
    //        var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;


    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageLocationUrl = map;
    //        claim.InvestigationReport.DigitalIdReport.Duration = duration;
    //        claim.InvestigationReport.DigitalIdReport.Distance = distance;
    //        claim.InvestigationReport.DigitalIdReport.DistanceInMetres = distanceInMetres;
    //        claim.InvestigationReport.DigitalIdReport.DurationInSeconds = durationInSecs;


    //        string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
    //            $"\r\n" +
    //            $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
    //            $"\r\n" +
    //            $"\r\nElevation(sea level):{weatherData.elevation} metres";

    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageData = weatherCustomData;
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImage = compressImage;
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageMatchConfidence = confidence;
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageLocationAddress = address;
    //        claim.InvestigationReport.DigitalIdReport.MatchExecuted = true;
    //        claim.InvestigationReport.DigitalIdReport.Similarity = similarity;
    //       var updateClaim = _context.ClaimsInvestigation.Update(claim);

    //        var rows = await _context.SaveChangesAsync();

    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

    //        var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
    //            LocationImage = updateClaim.Entity.InvestigationReport?.DigitalIdReport?.DigitalIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport?.DigitalIdReport?.DigitalIdImage) :
    //            Convert.ToBase64String(noDataimage),
    //            LocationLongLat = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageLongLat,
    //            LocationTime = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageLongLatTime,
    //            FacePercent = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageMatchConfidence
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.StackTrace);
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageData = "No Weather Data";
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImage = Convert.FromBase64String(data.LocationImage);
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageMatchConfidence = string.Empty;
    //        claim.InvestigationReport.DigitalIdReport.DigitalIdImageLocationAddress = "No Address data";
    //        claim.InvestigationReport.DigitalIdReport.MatchExecuted = true;
    //        var updateClaim = _context.ClaimsInvestigation.Update(claim);
    //        var rows = await _context.SaveChangesAsync();
    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");
    //        var noData = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
    //            LocationImage = updateClaim.Entity.InvestigationReport?.DigitalIdReport?.DigitalIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport?.DigitalIdReport?.DigitalIdImage) :
    //            Convert.ToBase64String(noData),
    //            LocationLongLat = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageLongLat,
    //            LocationTime = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageLongLatTime,
    //            FacePercent = claim.InvestigationReport.DigitalIdReport?.DigitalIdImageMatchConfidence
    //        };
    //    }
    //}

    //public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
    //{
    //    ClaimsInvestigation claim = null;
    //    Task<string> addressTask = null;
    //    try
    //    {
    //        claim = claimsService.GetClaims().Include(c => c.InvestigationReport).ThenInclude(c => c.PanIdReport).FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
    //        if (claim.InvestigationReport == null)
    //        {
    //            claim.InvestigationReport = new InvestigationReport();
    //        }
           
    //        var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;
    //        if (isClaim)
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date and time of death ?";
    //        }
    //        else
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
    //        }

    //        claim.InvestigationReport.AgentEmail = data.Email;
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat = data.OcrLongLat;
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageLongLatTime = DateTime.Now;
    //        var longLat = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.IndexOf("/");
    //        var latitude = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
    //        var longitude = claim.InvestigationReport.PanIdReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
    //        var latLongString = latitude + "," + longitude;
    //        var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
    //        var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

    //        var expectedLat = string.Empty;
    //        var expectedLong = string.Empty;
    //        if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
    //        {
    //            expectedLat = claim.CustomerDetail.Latitude;
    //            expectedLong = claim.CustomerDetail.Longitude;
    //        }
    //        else
    //        {
    //            expectedLat = claim.BeneficiaryDetail.Latitude;
    //            expectedLong = claim.BeneficiaryDetail.Longitude;
    //        }
    //        var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");

    //        #region PAN IMAGE PROCESSING

    //        //=================GOOGLE VISION API =========================

    //        var byteimage = Convert.FromBase64String(data.OcrImage);

    //        //await CompareFaces.DetectSampleAsync(byteimage);

    //        var googleDetecTask = googleApi.DetectTextAsync(byteimage);

    //        addressTask = httpClientService.GetRawAddress(latitude, longitude);

    //        await Task.WhenAll(googleDetecTask, addressTask, mapTask);

    //        var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;
    //        claim.InvestigationReport.PanIdReport.DistanceInMetres = distanceInMetres;
    //        claim.InvestigationReport.PanIdReport.DurationInSeconds = durationInSecs;
    //        claim.InvestigationReport.PanIdReport.Duration = duration;
    //        claim.InvestigationReport.PanIdReport.Distance = distance;
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageLocationUrl = map;

    //        var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);
    //        var imageReadOnly = await googleDetecTask;
    //        if (imageReadOnly != null && imageReadOnly.Count > 0)
    //        {
    //            var allPanText = imageReadOnly.FirstOrDefault().Description;
    //            var panTextPre = allPanText.IndexOf(panNumber2Find);
    //            var panNumber = allPanText.Substring(panTextPre + panNumber2Find.Length + 1, 10);


    //            var ocrImaged = googleHelper.MaskPanTextInImage(byteimage, imageReadOnly, panNumber2Find);
    //            var docyTypePan = allPanText.IndexOf(panNumber2Find) > 0 && allPanText.Length > allPanText.IndexOf(panNumber2Find) ? "PAN" : "UNKNOWN";
    //            var maskedImage = new FaceImageDetail
    //            {
    //                DocType = docyTypePan,
    //                DocumentId = panNumber,
    //                MaskedImage = Convert.ToBase64String(ocrImaged),
    //                OcrData = allPanText
    //            };
    //            try
    //            {
    //                #region// PAN VERIFICATION ::: //test PAN FNLPM8635N, BYSPP5796F
    //                if (company.VerifyPan)
    //                {
    //                    //var body = await httpClientService.VerifyPan(maskedImage.DocumentId, company.PanIdfyUrl, company.RapidAPIKey, company.RapidAPITaskId, company.RapidAPIGroupId);
    //                    //company.RapidAPIPanRemainCount = body?.count_remain;

    //                    //if (body != null && body?.status == "completed" &&
    //                    //    body?.result != null &&
    //                    //    body.result?.source_output != null
    //                    //    && body.result?.source_output?.status == "id_found")
    //                    var panResponse = await httpClientService.VerifyPanNew(maskedImage.DocumentId, company.PanIdfyUrl, company.PanAPIKey, company.PanAPIHost);
    //                    if (panResponse != null && panResponse.valid)
    //                    {
    //                        var panMatch = panRegex.Match(maskedImage.DocumentId);
    //                        claim.InvestigationReport.PanIdReport.DocumentIdImageValid = panMatch.Success && panResponse.valid ? true : false;
    //                    }
    //                }
    //                else
    //                {
    //                    var panMatch = panRegex.Match(maskedImage.DocumentId);
    //                    claim.InvestigationReport.PanIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
    //                }

    //                #endregion PAN IMAGE PROCESSING

    //                var image = Convert.FromBase64String(maskedImage.MaskedImage);
    //                var savedMaskedImage = CompressImage.ProcessCompress(image);
    //                claim.InvestigationReport.PanIdReport.DocumentIdImage = savedMaskedImage;
    //                claim.InvestigationReport.PanIdReport.DocumentIdImageType = maskedImage.DocType;
    //                claim.InvestigationReport.PanIdReport.DocumentIdImageData = maskedImage.DocType + " data: ";

    //                if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
    //                {
    //                    claim.InvestigationReport.PanIdReport.DocumentIdImageData = maskedImage.DocType + " data:. \r\n " +
    //                        "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine(ex.StackTrace);
    //                var image = Convert.FromBase64String(maskedImage.MaskedImage);
    //                claim.InvestigationReport.PanIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
    //                claim.InvestigationReport.PanIdReport.DocumentIdImageLongLatTime = DateTime.Now;
    //                claim.InvestigationReport.PanIdReport.DocumentIdImageData = "no data: ";
    //            }
    //        }
    //        //=================END GOOGLE VISION  API =========================

    //        else
    //        {
    //            var image = Convert.FromBase64String(data.OcrImage);
    //            claim.InvestigationReport.PanIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
    //            claim.InvestigationReport.PanIdReport.DocumentIdImageValid = false;
    //            claim.InvestigationReport.PanIdReport.DocumentIdImageLongLatTime = DateTime.Now;
    //            claim.InvestigationReport.PanIdReport.DocumentIdImageData = "no data: ";
    //        }

    //        #endregion PAN IMAGE PROCESSING
    //        var rawAddress = await addressTask;
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageLocationAddress = rawAddress;
    //        claim.InvestigationReport.PanIdReport.ValidationExecuted = true;

    //        _context.ClaimsInvestigation.Update(claim);

    //        var rows = await _context.SaveChangesAsync();

    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

    //        var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
    //            OcrImage = claim.InvestigationReport.PanIdReport?.DocumentIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport.PanIdReport?.DocumentIdImage) :
    //            Convert.ToBase64String(noDataimage),
    //            OcrLongLat = claim.InvestigationReport.PanIdReport?.DocumentIdImageLongLat,
    //            OcrTime = claim.InvestigationReport.PanIdReport?.DocumentIdImageLongLatTime,
    //            PanValid = claim.InvestigationReport.PanIdReport?.DocumentIdImageValid
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.StackTrace);
    //        var image = Convert.FromBase64String(data.OcrImage);
    //        claim.InvestigationReport.PanIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageLongLatTime = DateTime.Now;
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageData = "no data: ";
    //        var rawAddress = await addressTask;
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageLocationAddress = rawAddress;
    //        claim.InvestigationReport.PanIdReport.DocumentIdImageValid = false;
    //        _context.ClaimsInvestigation.Update(claim);

    //        var rows = await _context.SaveChangesAsync();
    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

    //        var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
    //            OcrImage = claim.InvestigationReport.PanIdReport?.DocumentIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport.PanIdReport?.DocumentIdImage) :
    //            Convert.ToBase64String(noDataimage),
    //            OcrLongLat = claim.InvestigationReport.PanIdReport?.DocumentIdImageLongLat,
    //            OcrTime = claim.InvestigationReport.PanIdReport?.DocumentIdImageLongLatTime,
    //            PanValid = claim.InvestigationReport.PanIdReport?.DocumentIdImageValid
    //        };
    //    }
    //}

    //public async Task<AppiCheckifyResponse> GetAudio(AudioData data)
    //{
    //    try
    //    {

    //        var claim = claimsService.GetClaims()
    //            .Include(c => c.InvestigationReport)
    //            .ThenInclude(c => c.AudioReport)
    //            .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
    //        if (claim.InvestigationReport == null)
    //        {
    //            claim.InvestigationReport = new InvestigationReport();
    //        }
    //        var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;

    //        if (isClaim)
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date and time of death ?";
    //        }
    //        else
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
    //        }

    //        claim.InvestigationReport.AgentEmail = data.Email;
    //        claim.InvestigationReport.AudioReport.DocumentIdImageLongLat = data.LongLat;
    //        claim.InvestigationReport.AudioReport.DocumentIdImageLongLatTime = DateTime.Now;
    //        var longLat = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.IndexOf("/");
    //        var latitude = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
    //        var longitude = claim.InvestigationReport.AudioReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
    //        var latLongString = latitude + "," + longitude;
    //        var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

    //        var expectedLat = string.Empty;
    //        var expectedLong = string.Empty;
    //        if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
    //        {
    //            expectedLat = claim.CustomerDetail.Latitude;
    //            expectedLong = claim.CustomerDetail.Longitude;
    //        }
    //        else
    //        {
    //            expectedLat = claim.BeneficiaryDetail.Latitude;
    //            expectedLong = claim.BeneficiaryDetail.Longitude;
    //        }
    //        var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A","X", "300", "300", "green", "red");


    //        var rawAddressTask = httpClientService.GetRawAddress(latitude, longitude);
    //        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
    //        var weatherDataTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);

    //        claim.InvestigationReport.AudioReport.ValidationExecuted = true;
    //        claim.InvestigationReport.AudioReport.DocumentIdImageValid = true;

    //        claim.InvestigationReport.AudioReport.DocumentIdImage = data.Mediabytes;

    //        //TO-DO: AWS : SPEECH TO TEXT;
    //        string audioDirectory = Path.Combine(webHostEnvironment.WebRootPath, "audio");
    //        if (!Directory.Exists(audioDirectory))
    //        {
    //            Directory.CreateDirectory(audioDirectory);
    //        }
    //        string audioFileName = "audio"+ DateTime.Now.ToString("ddMMMyyyy") + data.Name;
    //        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "audio", audioFileName);
    //        var audioFileTask = File.WriteAllBytesAsync(filePath, data.Mediabytes);

    //        var audio2Texttask = httpClientService.TranscribeAsync(audioS3FolderName, audioFileName, filePath);

    //        await Task.WhenAll(rawAddressTask, weatherDataTask, audioFileTask, audio2Texttask, weatherDataTask);
    //        var rawAddress = await rawAddressTask;
    //        claim.InvestigationReport.AudioReport.DocumentIdImageLocationAddress = rawAddress;
    //        await audioFileTask;
    //        var weatherData = await weatherDataTask;
    //        await audio2Texttask;
    //        var audioResult = await audio2Texttask;
    //        var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;

    //        claim.InvestigationReport.AudioReport.DistanceInMetres = distanceInMetres;
    //        claim.InvestigationReport.AudioReport.DurationInSeconds = durationInSecs;
    //        claim.InvestigationReport.AudioReport.Duration = duration;
    //        claim.InvestigationReport.AudioReport.Distance = distance;
    //        claim.InvestigationReport.AudioReport.DocumentIdImageLocationUrl = map;
    //        string audioData = "No data";
    //        if (weatherData != null && weatherData.current != null && weatherData.current.temperature_2m != null)
    //        {
    //            audioData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
    //            $"\r\n" +
    //            $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
    //            $"\r\n" +
    //            $"\r\nElevation(sea level):{weatherData.elevation} metres";
    //        }
    //        if (audioResult != null && audioResult.results != null && audioResult.results.audio_segments != null && audioResult.results.audio_segments.Count > 0)
    //        {
    //            audioData = audioData +  $"\r\n" +
    //                $"\r\n" +
    //            $"\r\nAudio Text:  :{string.Join(" ", audioResult.results.audio_segments.Select(s => s.transcript))}";
    //        }

    //        claim.InvestigationReport.AudioReport.DocumentIdImageData = audioData;
    //        claim.InvestigationReport.AudioReport.DocumentIdImagePath = filePath;
    //        //END :: TO-DO: AWS : SPEECH TO TEXT;

    //        var updatedClaim = _context.ClaimsInvestigation.Update(claim);
    //        await _context.SaveChangesAsync();
    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

    //        var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = updatedClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
    //            OcrImage = claim.InvestigationReport.AudioReport?.DocumentIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport.AudioReport?.DocumentIdImage) :
    //            Convert.ToBase64String(noDataimage),
    //            OcrLongLat = claim.InvestigationReport.AudioReport?.DocumentIdImageLongLat,
    //            OcrTime = claim.InvestigationReport.AudioReport?.DocumentIdImageLongLatTime,
    //            PanValid = claim.InvestigationReport.AudioReport?.DocumentIdImageValid
    //        };

    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.StackTrace);
    //        throw;
    //    }
    //}

    //public async Task<AppiCheckifyResponse> GetVideo(VideoData data)
    //{
    //    try
    //    {
    //        var claim = claimsService.GetClaims()
    //            .Include(c => c.InvestigationReport)
    //            .ThenInclude(c => c.VideoReport)
    //            .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
    //        if (claim.InvestigationReport == null)
    //        {
    //            claim.InvestigationReport = new InvestigationReport();
    //        }
    //        var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;

    //        if (isClaim)
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date and time of death ?";
    //        }
    //        else
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
    //        }

    //        claim.InvestigationReport.AgentEmail = data.Email;
    //        claim.InvestigationReport.VideoReport.DocumentIdImageLongLat = data.LongLat;
    //        claim.InvestigationReport.VideoReport.DocumentIdImageLongLatTime = DateTime.Now;
    //        var longLat = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.IndexOf("/");
    //        var latitude = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
    //        var longitude = claim.InvestigationReport.VideoReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
    //        var latLongString = latitude + "," + longitude;

    //        var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

    //        var expectedLat = string.Empty;
    //        var expectedLong = string.Empty;
    //        if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
    //        {
    //            expectedLat = claim.CustomerDetail.Latitude;
    //            expectedLong = claim.CustomerDetail.Longitude;
    //        }
    //        else
    //        {
    //            expectedLat = claim.BeneficiaryDetail.Latitude;
    //            expectedLong = claim.BeneficiaryDetail.Longitude;
    //        }
    //        var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");


    //        var rawAddressTask = httpClientService.GetRawAddress(latitude, longitude);
    //        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";

    //        var weatherDataTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);

    //        await Task.WhenAll(rawAddressTask, weatherDataTask, mapTask);

    //        var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;
    //        var rawAddress = await rawAddressTask;
    //        var weatherData = await weatherDataTask;


    //        string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
    //            $"\r\n" +
    //            $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
    //            $"\r\n" +
    //            $"\r\nElevation(sea level):{weatherData.elevation} metres";


    //        claim.InvestigationReport.VideoReport.DistanceInMetres = distanceInMetres;
    //        claim.InvestigationReport.VideoReport.DurationInSeconds = durationInSecs;
    //        claim.InvestigationReport.VideoReport.DocumentIdImageLocationUrl = map;
    //        claim.InvestigationReport.VideoReport.Duration = duration;
    //        claim.InvestigationReport.VideoReport.Distance = distance;
    //        claim.InvestigationReport.VideoReport.DocumentIdImageData = weatherCustomData;
    //        claim.InvestigationReport.VideoReport.DocumentIdImageLocationAddress = rawAddress;

    //        claim.InvestigationReport.VideoReport.ValidationExecuted = true;
    //        claim.InvestigationReport.VideoReport.DocumentIdImageValid = true;
    //        claim.InvestigationReport.VideoReport.DocumentIdImage = data.Mediabytes;

    //        //TO-DO: AWS : SPEECH TO TEXT;

    //        string videooDirectory = Path.Combine(webHostEnvironment.WebRootPath, "video");
    //        if (!Directory.Exists(videooDirectory))
    //        {
    //            Directory.CreateDirectory(videooDirectory);
    //        }
    //        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "video", data.Email + DateTime.Now.ToString("ddMMMyyyy") + data.Name);
    //        await File.WriteAllBytesAsync(filePath, data.Mediabytes);
    //        claim.InvestigationReport.VideoReport.DocumentIdImagePath = filePath;
    //        //END :: TO-DO: AWS : SPEECH TO TEXT;


    //        _context.ClaimsInvestigation.Update(claim);
    //        await _context.SaveChangesAsync();

    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

    //        var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
    //            OcrImage = claim.InvestigationReport.VideoReport?.DocumentIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport.VideoReport?.DocumentIdImage) :
    //            Convert.ToBase64String(noDataimage),
    //            OcrLongLat = claim.InvestigationReport.VideoReport?.DocumentIdImageLongLat,
    //            OcrTime = claim.InvestigationReport.VideoReport?.DocumentIdImageLongLatTime,
    //            PanValid = claim.InvestigationReport.VideoReport?.DocumentIdImageValid
    //        };

    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.StackTrace);
    //        throw;
    //    }
    //}

    //public async Task<AppiCheckifyResponse> GetPassportId(DocumentData data)
    //{
    //    try
    //    {
    //        var claim = claimsService.GetClaims().Include(c => c.InvestigationReport).ThenInclude(c => c.PassportIdReport).FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
    //        if (claim.InvestigationReport == null)
    //        {
    //            claim.InvestigationReport = new InvestigationReport();
    //        }
    //        var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;

    //        if (isClaim)
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date and time of death ?";
    //        }
    //        else
    //        {
    //            claim.InvestigationReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
    //            claim.InvestigationReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
    //        }

    //        claim.InvestigationReport.AgentEmail = data.Email;
    //        claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat = data.OcrLongLat;
    //        claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLatTime = DateTime.Now;
    //        var longLat = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.IndexOf("/");
    //        var latitude = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
    //        var longitude = claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
    //        var latLongString = latitude + "," + longitude;
    //        var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;


    //        var expectedLat = string.Empty;
    //        var expectedLong = string.Empty;
    //        if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness)
    //        {
    //            expectedLat = claim.CustomerDetail.Latitude;
    //            expectedLong = claim.CustomerDetail.Longitude;
    //        }
    //        else
    //        {
    //            expectedLat = claim.BeneficiaryDetail.Latitude;
    //            expectedLong = claim.BeneficiaryDetail.Longitude;
    //        }
    //        var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");
            
    //        var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);

    //        #region Passport IMAGE PROCESSING

    //        //=================GOOGLE VISION API =========================

    //        var byteimage = Convert.FromBase64String(data.OcrImage);

    //        var passportdataTask = httpClientService.GetPassportOcrResult(byteimage, company.PassportApiUrl, company.PassportApiKey, company.PassportApiHost);

    //        var googleDetecTask = googleApi.DetectTextAsync(byteimage);

    //        var addressTask = httpClientService.GetRawAddress(latitude, longitude);

    //        await Task.WhenAll(googleDetecTask, addressTask, passportdataTask, mapTask);

    //        var passportdata = await passportdataTask;
    //        var imageReadOnly = await googleDetecTask;
    //        if (imageReadOnly != null && imageReadOnly.Count > 0)
    //        {
    //            var allPassportText = imageReadOnly.FirstOrDefault().Description;
    //            var passportTextPre = allPassportText.IndexOf(passportNumber2Find);
    //            var dateOfBirth2FindTextPre = allPassportText.IndexOf(dateOfBirth2Find);

    //            var passportNumber = allPassportText.Substring(passportTextPre + passportNumber2Find.Length + 1, 8);
    //            var dateOfBirthNumber = allPassportText.Substring(dateOfBirth2FindTextPre + dateOfBirth2Find.Length + 1, 8);

    //            var passportMatch = passportRegex.Match(passportNumber);
    //            if (!passportMatch.Success)
    //            {
    //                passportNumber = passportRegex.Match(allPassportText).Value;
    //            }
    //            var dateOfBirth = dateOfBirthRegex.Match(allPassportText).Value;


    //            var ocrImaged = googleHelper.MaskPassportTextInImage(byteimage, imageReadOnly, passportNumber);
    //            var docyTypePassport = allPassportText.IndexOf(passportNumber2Find) > 0 && allPassportText.Length > allPassportText.IndexOf(passportNumber2Find) ? "Passport" : "UNKNOWN";
    //            var maskedImage = new FaceImageDetail
    //            {
    //                DocType = docyTypePassport,
    //                DocumentId = passportdata != null ? passportdata.data.ocr.documentNumber : passportNumber,
    //                MaskedImage = Convert.ToBase64String(ocrImaged),
    //                OcrData = allPassportText,
    //                DateOfBirth = passportdata != null ? passportdata.data.ocr.dateOfBirth : dateOfBirth
    //            };
    //            try
    //            {
    //                #region// PASSPORT VERIFICATION ::: //test 
    //                if (company.VerifyPassport)
    //                {
    //                    //to-do
    //                    var passportResponse = await httpClientService.VerifyPassport(maskedImage.DocumentId, maskedImage.DateOfBirth);
    //                    var panMatch = passportRegex.Match(maskedImage.DocumentId);
    //                    claim.InvestigationReport.PassportIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
    //                }
    //                else
    //                {
    //                    var panMatch = passportRegex.Match(maskedImage.DocumentId);
    //                    claim.InvestigationReport.PassportIdReport.DocumentIdImageValid = panMatch.Success ? true : false;
    //                }

    //                #endregion PAN IMAGE PROCESSING

    //                var image = Convert.FromBase64String(maskedImage.MaskedImage);
    //                var savedMaskedImage = CompressImage.ProcessCompress(image);
    //                claim.InvestigationReport.PassportIdReport.DocumentIdImage = savedMaskedImage;
    //                claim.InvestigationReport.PassportIdReport.DocumentIdImageType = maskedImage.DocType;
    //                claim.InvestigationReport.PassportIdReport.DocumentIdImageData = maskedImage.DocType + " data: ";

    //                if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
    //                {
    //                    claim.InvestigationReport.PassportIdReport.DocumentIdImageData = maskedImage.DocType + " data:. \r\n " +
    //                        "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine(ex.StackTrace);
    //                var image = Convert.FromBase64String(maskedImage.MaskedImage);
    //                claim.InvestigationReport.PassportIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
    //                claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLatTime = DateTime.Now;
    //                claim.InvestigationReport.PassportIdReport.DocumentIdImageData = "no data: ";
    //            }
    //        }
    //        //=================END GOOGLE VISION  API =========================

    //        else
    //        {
    //            var image = Convert.FromBase64String(data.OcrImage);
    //            claim.InvestigationReport.PassportIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
    //            claim.InvestigationReport.PassportIdReport.DocumentIdImageValid = false;
    //            claim.InvestigationReport.PassportIdReport.DocumentIdImageLongLatTime = DateTime.Now;
    //            claim.InvestigationReport.PassportIdReport.DocumentIdImageData = "no data: ";
    //        }

    //        #endregion PAN IMAGE PROCESSING
    //        var rawAddress = await addressTask;
    //        var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;
    //        claim.InvestigationReport.PassportIdReport.DistanceInMetres = distanceInMetres;
    //        claim.InvestigationReport.PassportIdReport.DurationInSeconds = durationInSecs;
    //        claim.InvestigationReport.PassportIdReport.Duration = duration;
    //        claim.InvestigationReport.PassportIdReport.Distance = distance;
    //        claim.InvestigationReport.PassportIdReport.DocumentIdImageLocationAddress = rawAddress;
    //        claim.InvestigationReport.PassportIdReport.ValidationExecuted = true;

    //        _context.ClaimsInvestigation.Update(claim);

    //        var rows = await _context.SaveChangesAsync();

    //        var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

    //        var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
    //        return new AppiCheckifyResponse
    //        {
    //            BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
    //            OcrImage = claim.InvestigationReport.PassportIdReport?.DocumentIdImage != null ?
    //            Convert.ToBase64String(claim.InvestigationReport.PassportIdReport?.DocumentIdImage) :
    //            Convert.ToBase64String(noDataimage),
    //            OcrLongLat = claim.InvestigationReport.PassportIdReport?.DocumentIdImageLongLat,
    //            OcrTime = claim.InvestigationReport.PassportIdReport?.DocumentIdImageLongLatTime,
    //            PanValid = claim.InvestigationReport.PassportIdReport?.DocumentIdImageValid
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.StackTrace);
    //        throw;
    //    }
    //}
}