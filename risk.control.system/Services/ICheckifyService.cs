using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using System.Net.Http;

using risk.control.system.Models.ViewModel;
using risk.control.system.Controllers.Api;
using risk.control.system.Data;
using risk.control.system.AppConstant;

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
                this.logger.LogInformation("DIGITAL ID : FACE image {LocationImage} ", data.LocationImage);

                if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                {
                    registeredImage = claim.CustomerDetail.ProfilePicture;
                    this.logger.LogInformation("DIGITAL ID : HEALTH image {registeredImage} ", registeredImage);
                }
                if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
                {
                    registeredImage = claimCase.ProfilePicture;
                    this.logger.LogInformation("DIGITAL ID : DEATH image {registeredImage} ", registeredImage);
                }

                string ImageData = string.Empty;
                try
                {
                    if (registeredImage != null)
                    {
                        var image = Convert.FromBase64String(data.LocationImage);
                        var locationRealImage = ByteArrayToImage(image);
                        MemoryStream stream = new MemoryStream(image);
                        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"loc{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{locationRealImage.ImageType()}");
                        claimCase.ClaimReport.DigitalIdImagePath = filePath;
                        CompressImage.Compressimage(stream, filePath);

                        var savedImage = await File.ReadAllBytesAsync(filePath);
                        claimCase.ClaimReport.DigitalIdImage = savedImage;
                        var saveImageBase64String = Convert.ToBase64String(savedImage);

                        claimCase.ClaimReport.DigitalIdLongLatTime = DateTime.UtcNow;
                        this.logger.LogInformation("DIGITAL ID : saved image {registeredImage} ", registeredImage);

                        var base64Image = Convert.ToBase64String(registeredImage);

                        this.logger.LogInformation("DIGITAL ID : HEALTH image {base64Image} ", base64Image);
                        try
                        {
                            var faceImageDetail = await httpClientService.GetFaceMatch(new MatchImage { Source = base64Image, Dest = saveImageBase64String }, company.ApiBaseUrl);

                            claimCase.ClaimReport.DigitalIdImageMatchConfidence = faceImageDetail?.Confidence;
                        }
                        catch (Exception)
                        {
                            claimCase.ClaimReport.DigitalIdImageMatchConfidence = string.Empty;
                        }
                    }
                    else
                    {
                        claimCase.ClaimReport.DigitalIdImageMatchConfidence = "no face image";
                    }
                }
                catch (Exception ex)
                {
                    claimCase.ClaimReport.DigitalIdImageMatchConfidence = "err " + ImageData;
                }
                if (registeredImage == null)
                {
                    claimCase.ClaimReport.DigitalIdImageMatchConfidence = "no image";
                }
            }

            #endregion FACE IMAGE PROCESSING

            if (!string.IsNullOrWhiteSpace(data.LocationLongLat))
            {
                claimCase.ClaimReport.DigitalIdLongLatTime = DateTime.UtcNow;
                claimCase.ClaimReport.DigitalIdLongLat = data.LocationLongLat;
            }

            if (!string.IsNullOrWhiteSpace(claimCase.ClaimReport.DigitalIdLongLat))
            {
                var longLat = claimCase.ClaimReport.DigitalIdLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.DigitalIdLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.DigitalIdLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
                string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                    $"\r\n" +
                    $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                    $"\r\n" +
                    $"\r\nElevation(sea level):{weatherData.elevation} metres";
                claimCase.ClaimReport.DigitalIdImageData = weatherCustomData;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                claimCase.ClaimReport.DigitalIdImageLocationUrl = url;

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

                claimCase.ClaimReport.DigitalIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
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
                LocationImage = !string.IsNullOrWhiteSpace(claimCase.ClaimReport.DigitalIdImagePath) ?
                Convert.ToBase64String(File.ReadAllBytes(claimCase.ClaimReport.DigitalIdImagePath)) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claimCase.ClaimReport.DigitalIdLongLat,
                LocationTime = claimCase.ClaimReport.DigitalIdLongLatTime,
                OcrImage = !string.IsNullOrWhiteSpace(claimCase.ClaimReport.DocumentIdImagePath) ?
                Convert.ToBase64String(File.ReadAllBytes(claimCase.ClaimReport.DocumentIdImagePath)) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claimCase.ClaimReport.DocumentIdImageLongLat,
                OcrTime = claimCase.ClaimReport.DocumentIdImageLongLatTime,
                FacePercent = claimCase.ClaimReport.DigitalIdImageMatchConfidence,
                PanValid = claimCase.ClaimReport.DocumentIdImageValid
            };
        }

        private System.Drawing.Image? ByteArrayToImage(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }

        public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
        {
            var claimCase = _context.CaseLocation
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
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

                var locationRealImage = ByteArrayToImage(byteimage);
                MemoryStream mstream = new MemoryStream(byteimage);
                var mfilePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"loc{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{locationRealImage.ImageType()}");
                claimCase.ClaimReport.DocumentIdImagePath = mfilePath;
                CompressImage.Compressimage(mstream, mfilePath);

                var savedImage = await File.ReadAllBytesAsync(mfilePath);

                var base64Image = Convert.ToBase64String(savedImage);
                var inputImage = new MaskImage { Image = base64Image };

                this.logger.LogInformation("DOCUMENT ID : PAN image {ocrImage} ", data.OcrImage);

                var maskedImage = await httpClientService.GetMaskedImage(inputImage, company.ApiBaseUrl);

                this.logger.LogInformation("DOCUMENT ID : PAN maskedImage image {maskedImage} ", maskedImage);
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
                                        claimCase.ClaimReport.DocumentIdImageValid = true;
                                    }
                                    else
                                    {
                                        claimCase.ClaimReport.DocumentIdImageValid = false;
                                    }
                                }
                                catch (Exception)
                                {
                                    claimCase.ClaimReport.DocumentIdImageValid = false;
                                }
                            }
                            else
                            {
                                claimCase.ClaimReport.DocumentIdImageValid = true;
                            }
                        }

                        #endregion PAN IMAGE PROCESSING

                        var image = Convert.FromBase64String(maskedImage.MaskedImage);
                        var OcrRealImage = ByteArrayToImage(image);
                        MemoryStream stream = new MemoryStream(image);
                        claimCase.ClaimReport.DocumentIdImage = image;
                        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"{maskedImage.DocType}{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{OcrRealImage.ImageType()}");
                        CompressImage.Compressimage(stream, filePath);
                        claimCase.ClaimReport.DocumentIdImagePath = filePath;
                        claimCase.ClaimReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                        claimCase.ClaimReport.DocumentIdImageType = maskedImage.DocType;
                        claimCase.ClaimReport.DocumentIdImageData = maskedImage.DocType + " data: ";

                        if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                        {
                            claimCase.ClaimReport.DocumentIdImageData = claimCase.ClaimReport.DocumentIdImageData + ". \r\n " +
                                "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                        }
                    }
                    catch (Exception)
                    {
                        var image = Convert.FromBase64String(maskedImage.MaskedImage);
                        var OcrRealImage = ByteArrayToImage(image);
                        MemoryStream stream = new MemoryStream(image);
                        claimCase.ClaimReport.DocumentIdImage = image;
                        var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"{maskedImage.DocType}{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{OcrRealImage.ImageType()}");
                        claimCase.ClaimReport.DocumentIdImagePath = filePath;
                        CompressImage.Compressimage(stream, filePath);
                        claimCase.ClaimReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                    }
                }
                else
                {
                    this.logger.LogInformation("DOCUMENT ID : PAN maskedImage image {maskedImage} ", maskedImage);
                    var image = Convert.FromBase64String(data.OcrImage);
                    var OcrRealImage = ByteArrayToImage(image);
                    MemoryStream stream = new MemoryStream(image);
                    claimCase.ClaimReport.DocumentIdImage = image;
                    var filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", $"ocr{DateTime.UtcNow.ToString("dd-MMM-yyyy-HH-mm-ss")}.{OcrRealImage.ImageType()}");
                    CompressImage.Compressimage(stream, filePath);
                    claimCase.ClaimReport.DocumentIdImagePath = filePath;
                    claimCase.ClaimReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                    claimCase.ClaimReport.DocumentIdImageData = "no data: ";
                }
            }

            #endregion PAN IMAGE PROCESSING

            if (!string.IsNullOrWhiteSpace(data.OcrLongLat))
            {
                claimCase.ClaimReport.DocumentIdImageLongLat = data.OcrLongLat;
                claimCase.ClaimReport.DocumentIdImageLongLatTime = DateTime.UtcNow;
                var longLat = claimCase.ClaimReport.DocumentIdImageLongLat.IndexOf("/");
                var latitude = claimCase.ClaimReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                var longitude = claimCase.ClaimReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                var latLongString = latitude + "," + longitude;
                var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=100x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                claimCase.ClaimReport.DocumentIdImageLocationUrl = url;
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

                claimCase.ClaimReport.DocumentIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
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
                LocationImage = !string.IsNullOrWhiteSpace(claimCase.ClaimReport.DigitalIdImagePath) ?
                Convert.ToBase64String(System.IO.File.ReadAllBytes(claimCase.ClaimReport.DigitalIdImagePath)) :
                Convert.ToBase64String(noDataimage),
                LocationLongLat = claimCase.ClaimReport.DigitalIdLongLat,
                LocationTime = claimCase.ClaimReport.DigitalIdLongLatTime,
                OcrImage = !string.IsNullOrWhiteSpace(claimCase.ClaimReport.DocumentIdImagePath) ?
                Convert.ToBase64String(System.IO.File.ReadAllBytes(claimCase.ClaimReport.DocumentIdImagePath)) :
                Convert.ToBase64String(noDataimage),
                OcrLongLat = claimCase.ClaimReport.DocumentIdImageLongLat,
                OcrTime = claimCase.ClaimReport.DocumentIdImageLongLatTime,
                FacePercent = claimCase.ClaimReport.DigitalIdImageMatchConfidence,
                PanValid = claimCase.ClaimReport.DocumentIdImageValid
            };
        }
    }
}