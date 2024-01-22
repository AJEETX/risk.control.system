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

namespace risk.control.system.Services
{
    public interface IICheckifyService
    {
        Task<AppiCheckifyResponse> GetFaceId(FaceData data);

        Task<AppiCheckifyResponse> GetDocumentId(DocumentData data);
    }

    public class ICheckifyService : IICheckifyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientService httpClientService;
        private readonly IClaimsInvestigationService claimsInvestigationService;
        private readonly IMailboxService mailboxService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static HttpClient httpClient = new();

        private ILogger<AgentController> logger;

        //test PAN FNLPM8635N
        public ICheckifyService(ApplicationDbContext context, IHttpClientService httpClientService, IClaimsInvestigationService claimsInvestigationService, IMailboxService mailboxService, IWebHostEnvironment webHostEnvironment, ILogger<AgentController> logger)
        {
            this._context = context;
            this.httpClientService = httpClientService;
            this.claimsInvestigationService = claimsInvestigationService;
            this.mailboxService = mailboxService;
            this.webHostEnvironment = webHostEnvironment;
            this.logger = logger;
        }

        public async Task<AppiCheckifyResponse> GetFaceId(FaceData data)
        {
            var claimCase = _context.CaseLocation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            if (claimCase == null)
            {
                return null;
            }

            claimCase.ClaimReport.AgentEmail = data.Email;

            var claim = _context.ClaimsInvestigation
            .Include(c => c.PolicyDetail)
            .Include(c => c.CustomerDetail)
            .ThenInclude(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.PolicyDetail.ClientCompanyId);

            #region FACE IMAGE PROCESSING

            //var (claimWithDigitalId, ClaimCaseWithDitialId) = await ProcessDigitalId(data, claim, claimCase);
            if (!string.IsNullOrWhiteSpace(data.LocationImage))
            {
                byte[]? registeredImage = null;

                if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredImage = claim.CustomerDetail.ProfilePicture;
                }
                if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
                {
                    registeredImage = claimCase.ProfilePicture;
                }

                string ImageData = string.Empty;
                try
                {
                    if (registeredImage != null)
                    {
                        var image = Convert.FromBase64String(data.LocationImage);

                        MemoryStream stream = new MemoryStream(image);
                        string path = Path.Combine(webHostEnvironment.WebRootPath, "verify");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "verify", $"face{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.jpg");
                        CompressImage.CompressimageWindows(stream, filePath);

                        claimCase.ClaimReport.DigitalIdReport.DigitalIdImagePath = filePath;

                        var savedImage = await File.ReadAllBytesAsync(filePath);

                        //var savedImage = CompressImage.Compress(stream.ToArray());

                        var saveImageBase64String = Convert.ToBase64String(savedImage);

                        claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLatTime = DateTime.UtcNow;

                        var base64Image = Convert.ToBase64String(registeredImage);

                        try
                        {
                            var faceImageDetail = await httpClientService.GetFaceMatch(new MatchImage { Source = base64Image, Dest = saveImageBase64String }, company.ApiBaseUrl);

                            claimCase.ClaimReport.DigitalIdReport.DigitalIdImage = CompressImage.ProcessCompress(stream.ToArray());

                            claimCase.ClaimReport.DigitalIdReport.DigitalIdImageMatchConfidence = faceImageDetail?.Confidence;
                        }
                        catch (Exception)
                        {
                            claimCase.ClaimReport.DigitalIdReport.DigitalIdImageMatchConfidence = string.Empty;
                        }
                    }
                    else
                    {
                        claimCase.ClaimReport.DigitalIdReport.DigitalIdImageMatchConfidence = "no face image";
                    }
                }
                catch (Exception ex)
                {
                    claimCase.ClaimReport.DigitalIdReport.DigitalIdImageMatchConfidence = "err " + ImageData;
                }
                if (registeredImage == null)
                {
                    claimCase.ClaimReport.DigitalIdReport.DigitalIdImageMatchConfidence = "no image";
                }
            }

            #endregion FACE IMAGE PROCESSING

            if (!string.IsNullOrWhiteSpace(data.LocationLongLat))
            {
                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLatTime = DateTime.UtcNow;
                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat = data.LocationLongLat;
            }

            if (!string.IsNullOrWhiteSpace(claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat))
            {
                var longLat = claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
                string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                    $"\r\n" +
                    $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                    $"\r\n" +
                    $"\r\nElevation(sea level):{weatherData.elevation} metres";
                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageData = weatherCustomData;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLocationUrl = url;

                var rootObject = await httpClientService.GetAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Longitude);
                }
                else
                {
                    registeredLatitude = Convert.ToDouble(claimCase.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claimCase.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                claimCase.ClaimReport.DigitalIdReport.DigitalIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
                claimCase.ClaimReport.DigitalIdReport.Updated = DateTime.UtcNow;
                claimCase.ClaimReport.DigitalIdReport.UpdatedBy = claimCase.ClaimReport.AgentEmail;
            }

            _context.CaseLocation.Update(claimCase);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                throw exception;
            }

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claimCase.CaseLocationId,
                LocationImage = claimCase.ClaimReport?.DigitalIdReport?.DigitalIdImage != null ?
                Convert.ToBase64String(claimCase.ClaimReport?.DigitalIdReport?.DigitalIdImage) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claimCase.ClaimReport.DigitalIdReport?.DigitalIdImageLongLat,
                LocationTime = claimCase.ClaimReport.DigitalIdReport?.DigitalIdImageLongLatTime,
                OcrImage = claimCase.ClaimReport?.DocumentIdReport?.DocumentIdImage != null ?
                Convert.ToBase64String((claimCase.ClaimReport?.DocumentIdReport?.DocumentIdImage)) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claimCase.ClaimReport.DocumentIdReport?.DocumentIdImageLongLat,
                OcrTime = claimCase.ClaimReport.DocumentIdReport?.DocumentIdImageLongLatTime,
                FacePercent = claimCase.ClaimReport.DigitalIdReport?.DigitalIdImageMatchConfidence,
                PanValid = claimCase.ClaimReport.DocumentIdReport?.DocumentIdImageValid
            };
        }

        public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
        {
            var claimCase = _context.CaseLocation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.ClaimReport)
                .ThenInclude(c => c.DocumentIdReport)
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            if (claimCase == null)
            {
                return null;
            }

            claimCase.ClaimReport.AgentEmail = data.Email;

            var claim = _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.PolicyDetail.ClientCompanyId);

            #region PAN IMAGE PROCESSING

            if (!string.IsNullOrWhiteSpace(data.OcrImage))
            {
                var byteimage = Convert.FromBase64String(data.OcrImage);

                MemoryStream stream = new MemoryStream(byteimage);
                string path = Path.Combine(webHostEnvironment.WebRootPath, "verify");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var mfilePath = Path.Combine(webHostEnvironment.WebRootPath, "verify", $"pan{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.jpg");
                CompressImage.CompressimageWindows(stream, mfilePath);

                claimCase.ClaimReport.DocumentIdReport.DocumentIdImagePath = mfilePath;

                var savedImage = await File.ReadAllBytesAsync(mfilePath);
                //var savedImage = CompressImage.Compress(stream.ToArray());

                var base64Image = Convert.ToBase64String(byteimage);
                var inputImage = new MaskImage { Image = base64Image };

                var maskedImage = await httpClientService.GetMaskedImage(inputImage, company.ApiBaseUrl);

                if (maskedImage != null)
                {
                    try
                    {
                        #region// PAN VERIFICATION ::: //test PAN FNLPM8635N, BYSPP5796F

                        if (maskedImage.DocType.ToUpper() == "PAN")
                        {
                            if (company.VerifyOcr)
                            {
                                try
                                {
                                    var body = await httpClientService.VerifyPan(maskedImage.DocumentId, company.PanIdfyUrl, company.RapidAPIKey, company.RapidAPITaskId, company.RapidAPIGroupId);
                                    company.RapidAPIPanRemainCount = body.count_remain;

                                    if (body != null && body?.status == "completed" &&
                                        body?.result != null &&
                                        body.result?.source_output != null
                                        && body.result?.source_output?.status == "id_found")
                                    {
                                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImageValid = true;
                                    }
                                    else
                                    {
                                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImageValid = false;
                                    }
                                }
                                catch (Exception)
                                {
                                    claimCase.ClaimReport.DocumentIdReport.DocumentIdImageValid = false;
                                }
                            }
                            else
                            {
                                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageValid = true;
                            }
                        }

                        #endregion PAN IMAGE PROCESSING

                        var image = Convert.FromBase64String(maskedImage.MaskedImage);

                        var savedMaskedImage = CompressImage.ProcessCompress(image);

                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImage = savedMaskedImage;

                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImageType = maskedImage.DocType;
                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImageData = maskedImage.DocType + " data: ";

                        if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                        {
                            claimCase.ClaimReport.DocumentIdReport.DocumentIdImageData = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageData + ". \r\n " +
                                "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                        }
                    }
                    catch (Exception)
                    {
                        var image = Convert.FromBase64String(maskedImage.MaskedImage);
                        var savedMaskedImage = CompressImage.Compress(image);

                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImage = savedMaskedImage;

                        claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                    }
                }
                else
                {
                    var image = Convert.FromBase64String(data.OcrImage);
                    var savedMaskedImage = CompressImage.Compress(image);

                    claimCase.ClaimReport.DocumentIdReport.DocumentIdImage = savedMaskedImage;

                    claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                    claimCase.ClaimReport.DocumentIdReport.DocumentIdImageData = "no data: ";
                }
            }

            #endregion PAN IMAGE PROCESSING

            if (!string.IsNullOrWhiteSpace(data.OcrLongLat))
            {
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLat = data.OcrLongLat;
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                var longLat = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLocationUrl = url;
                RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
                double registeredLatitude = 0;
                double registeredLongitude = 0;
                if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredLatitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Latitude);
                    registeredLongitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Longitude);
                }
                var distance = DistanceFinder.GetDistance(registeredLatitude, registeredLongitude, Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                var address = rootObject.display_name;

                claimCase.ClaimReport.DocumentIdReport.DocumentIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
            }
            claimCase.ClaimReport.DocumentIdReport.Updated = DateTime.UtcNow;
            claimCase.ClaimReport.DocumentIdReport.UpdatedBy = claimCase.ClaimReport.AgentEmail;
            _context.CaseLocation.Update(claimCase);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                throw exception;
            }

            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claimCase.CaseLocationId,
                LocationImage = claimCase.ClaimReport.DigitalIdReport?.DigitalIdImage != null ?
                Convert.ToBase64String(claimCase.ClaimReport.DigitalIdReport?.DigitalIdImage) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claimCase.ClaimReport.DigitalIdReport?.DigitalIdImageLongLat,
                LocationTime = claimCase.ClaimReport.DigitalIdReport?.DigitalIdImageLongLatTime,
                OcrImage = claimCase.ClaimReport.DocumentIdReport?.DocumentIdImage != null ?
                Convert.ToBase64String(claimCase.ClaimReport.DocumentIdReport?.DocumentIdImage) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claimCase.ClaimReport.DocumentIdReport?.DocumentIdImageLongLat,
                OcrTime = claimCase.ClaimReport.DocumentIdReport?.DocumentIdImageLongLatTime,
                FacePercent = claimCase.ClaimReport.DigitalIdReport?.DigitalIdImageMatchConfidence,
                PanValid = claimCase.ClaimReport.DocumentIdReport?.DocumentIdImageValid
            };
        }
    }
}