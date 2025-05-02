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
        AgentIdReport face = null;
        LocationTemplate location = null;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationTemplate)
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
                .FirstOrDefaultAsync(c => c.Id == data.CaseId);

            if (claim.InvestigationReport == null)
            {
                return null;
            }
            var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == data.Email);

            location = claim.InvestigationReport.ReportTemplate.LocationTemplate.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationTemplate
                .Include(l => l.AgentIdReport)
                .Include(l => l.FaceIds)
                .Include(l => l.DocumentIds)
                .Include(l => l.Questions)
                .FirstOrDefault(l => l.Id == location.Id);

            var isAgentReportName = data.ReportName == CONSTANTS.LOCATIONS.AGENT_PHOTO;

            face = locationTemplate.AgentIdReport;

            using (var stream = new MemoryStream())
            {
                await data.Image.CopyToAsync(stream);
                face.IdImage = stream.ToArray();
            }

            location.AgentEmail = agent.Email;
            _context.LocationTemplate.Update(location);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.IdImageLongLatTime = DateTime.Now;
            face.IdImageLongLat = data.LocationLatLong;
            var longLat = face.IdImageLongLat.IndexOf("/");
            var latitude = face.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = face.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            byte[]? registeredImage = null;
            if (data.Type == "0")
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

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, face.IdImage);
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
            face.IdImageValid = similarity > 70;
            _context.AgentIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();

            
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = face.IdImage,
                LocationImage = Convert.ToBase64String(face.IdImage),
                LocationLongLat = face.IdImageLongLat,
                LocationTime = face?.IdImageLongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            face.IdImageData = "No Weather Data";
            face.DigitalIdImageMatchConfidence = string.Empty;
            face.IdImageLocationAddress = "No Address data";
            face.ValidationExecuted = true;
            _context.AgentIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = face.IdImage,
                LocationImage = Convert.ToBase64String(face?.IdImage),
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
        LocationTemplate location = null;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationTemplate)
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
                .FirstOrDefaultAsync(c => c.Id == data.CaseId);

            if (claim.InvestigationReport == null)
            {
                return null;
            }
            var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == data.Email);

            location = claim.InvestigationReport.ReportTemplate.LocationTemplate.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationTemplate
                .Include(l => l.AgentIdReport)
                .Include(l => l.FaceIds)
                .Include(l => l.DocumentIds)
                .Include(l => l.Questions)
                .FirstOrDefault(l => l.Id == location.Id);

            face = locationTemplate.FaceIds.FirstOrDefault(c => c.ReportName == data.ReportName);

            using (var stream = new MemoryStream())
            {
                await data.Image.CopyToAsync(stream);
                face.IdImage = stream.ToArray();
            }

            location.AgentEmail = agent.Email;
            _context.LocationTemplate.Update(location);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.IdImageLongLatTime = DateTime.Now;
            face.IdImageLongLat = data.LocationLatLong;
            var longLat = face.IdImageLongLat.IndexOf("/");
            var latitude = face.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = face.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            byte[]? registeredImage = null;
            if (data.Type == "0")
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

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, face.IdImage);
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
            face.IdImageValid = similarity > 70;
            _context.DigitalIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = face.IdImage,
                LocationImage = Convert.ToBase64String(face.IdImage),
                LocationLongLat = face.IdImageLongLat,
                LocationTime = face?.IdImageLongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            face.IdImageData = "No Weather Data";
            face.DigitalIdImageMatchConfidence = string.Empty;
            face.IdImageLocationAddress = "No Address data";
            face.ValidationExecuted = true;
            face.IdImageValid = false;
            _context.DigitalIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = face.IdImage,
                LocationImage = Convert.ToBase64String( face?.IdImage),
                LocationLongLat = face?.IdImageLongLat,
                LocationTime = face?.IdImageLongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
    }

    public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
    {
        InvestigationTask claim = null;
        DocumentIdReport doc = null;
        Task<string> addressTask = null;
        
        try
        {
            claim = await _context.Investigations
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationTemplate)
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
                .FirstOrDefaultAsync(c => c.Id == data.CaseId);

            var location = claim.InvestigationReport.ReportTemplate.LocationTemplate.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationTemplate
                .Include(l => l.AgentIdReport)
                .Include(l => l.FaceIds)
                .Include(l => l.DocumentIds)
                .Include(l => l.Questions)
                .FirstOrDefault(l => l.Id == location.Id);

            doc = locationTemplate.DocumentIds.FirstOrDefault(c => c.ReportName == data.ReportName);
            using (var stream = new MemoryStream())
            {
                await data.Image.CopyToAsync(stream);
                doc.IdImage = stream.ToArray();
            }

            doc.IdImageLongLat = data.LocationLatLong;
            doc.IdImageLongLatTime = DateTime.Now;
            var longLat = doc.IdImageLongLat.IndexOf("/");
            var latitude = doc.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = doc.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
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

            var googleDetecTask = googleApi.DetectTextAsync(doc.IdImage);

            addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(googleDetecTask, addressTask, mapTask);

            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;
            doc.DistanceInMetres = distanceInMetres;
            doc.DurationInSeconds = durationInSecs;
            doc.Duration = duration;
            doc.Distance = distance;
            doc.IdImageLocationUrl = map;

            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);
            var imageReadOnly = await googleDetecTask;
            if (imageReadOnly != null && imageReadOnly.Count > 0)
            {
                var allPanText = imageReadOnly.FirstOrDefault().Description;
                var panTextPre = allPanText.IndexOf(panNumber2Find);
                var panNumber = allPanText.Substring(panTextPre + panNumber2Find.Length + 1, 10);


                var ocrImaged = googleHelper.MaskPanTextInImage(doc.IdImage, imageReadOnly, panNumber2Find);
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
                        var panResponse = await httpClientService.VerifyPanNew(maskedImage.DocumentId, company.PanIdfyUrl, company.PanAPIKey, company.PanAPIHost);
                        if (panResponse != null && panResponse.valid)
                        {
                            var panMatch = panRegex.Match(maskedImage.DocumentId);
                            doc.IdImageValid = panMatch.Success && panResponse.valid ? true : false;
                        }
                    }
                    else
                    {
                        var panMatch = panRegex.Match(maskedImage.DocumentId);
                        doc.IdImageValid = panMatch.Success ? true : false;
                    }

                    #endregion PAN IMAGE PROCESSING

                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    var savedMaskedImage = CompressImage.ProcessCompress(image);
                    doc.IdImage = savedMaskedImage;
                    doc.IdImageData = maskedImage.DocType + " data: ";

                    if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                    {
                        doc.IdImageData = maskedImage.DocType + " data:. \r\n " +
                            "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    var image = Convert.FromBase64String(maskedImage.MaskedImage);
                    doc.IdImage = CompressImage.ProcessCompress(image);
                    doc.IdImageLongLatTime = DateTime.Now;
                    doc.IdImageData = "no data: ";
                }
            }
            //=================END GOOGLE VISION  API =========================

            else
            {
                doc.IdImage = CompressImage.ProcessCompress(doc.IdImage);
                doc.IdImageValid = false;
                doc.IdImageLongLatTime = DateTime.Now;
                doc.IdImageData = "no data: ";
            }

            #endregion PAN IMAGE PROCESSING
            var rawAddress = await addressTask;
            doc.IdImageLocationAddress = rawAddress;
            doc.ValidationExecuted = true;

            _context.Investigations.Update(claim);

            var rows = await _context.SaveChangesAsync();

           
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                Image = doc.IdImage,
                OcrImage = Convert.ToBase64String(doc?.IdImage),
                OcrLongLat = doc?.IdImageLongLat,
                OcrTime = doc?.IdImageLongLatTime,
                Valid = doc?.IdImageValid
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            doc.IdImageData = "No Weather Data";
            doc.IdImageValid = false;
            doc.IdImageLocationAddress = "No Address data";
            doc.ValidationExecuted = true;
            _context.DocumentIdReport.Update(doc);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = doc.IdImage,
                LocationImage = Convert.ToBase64String(doc?.IdImage),
                LocationLongLat = doc?.IdImageLongLat,
                LocationTime = doc?.IdImageLongLatTime,
                Valid = doc?.IdImageValid
            };
        }
    }
}