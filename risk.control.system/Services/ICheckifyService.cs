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

namespace risk.control.system.Services
{
    public interface IICheckifyService
    {
        Task<AppiCheckifyResponse> GetFaceId(FaceData data);

        Task<AppiCheckifyResponse> GetDocumentId(DocumentData data);

        Task GetAudio(AudioData data);

        Task GetVideo(VideoData data);
        Task<bool> WhitelistIP(IPWhitelistRequest request);
    }

    public class ICheckifyService : IICheckifyService
    {
        private static Regex panRegex = new Regex(@"[A-Z]{5}\d{4}[A-Z]{1}");
        private static string txt2Find = "Permanent Account Number";
        private readonly ApplicationDbContext _context;
        private readonly IGoogleApi googleApi;
        private readonly IGoogleMaskHelper googleHelper;
        private readonly IHttpClientService httpClientService;
        private readonly IClaimsService claimsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private static HttpClient httpClient = new();

        //test PAN FNLPM8635N
        public ICheckifyService(ApplicationDbContext context, IGoogleApi googleApi,
            IGoogleMaskHelper googleHelper, IHttpClientService httpClientService,
            IClaimsService claimsService,
            IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.googleApi = googleApi;
            this.googleHelper = googleHelper;
            this.httpClientService = httpClientService;
            this.claimsService = claimsService;
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
                var claim = claimsService.GetClaims()
                                .Include(c => c.AgencyReport)
                                .ThenInclude(c => c.DigitalIdReport)
                                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
                
                if(claim.AgencyReport == null)
                {
                    claim.AgencyReport = new AgencyReport();
                }
                claim.AgencyReport.AgentEmail = data.Email;

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.PolicyDetail.ClientCompanyId);

                #region FACE IMAGE PROCESSING

                if (!string.IsNullOrWhiteSpace(data.LocationImage))
                {
                    byte[]? registeredImage = null;

                    if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                    {
                        registeredImage = claim.CustomerDetail.ProfilePicture;
                    }
                    if (claim.PolicyDetail.ClaimType == ClaimType.DEATH)
                    {
                        registeredImage = claim.BeneficiaryDetail.ProfilePicture;
                    }

                    string ImageData = string.Empty;
                    try
                    {
                        if (registeredImage != null)
                        {
                            var face2Verify = Convert.FromBase64String(data.LocationImage);

                            claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime = DateTime.Now;

                            try
                            {

                                var matched = await CompareFaces.Do(registeredImage, face2Verify);

                                if (matched)
                                {
                                    claim.AgencyReport.DigitalIdReport.DigitalIdImageMatchConfidence = "99";
                                }
                                else
                                {
                                    claim.AgencyReport.DigitalIdReport.DigitalIdImageMatchConfidence = string.Empty;
                                }
                                claim.AgencyReport.DigitalIdReport.DigitalIdImage = CompressImage.ProcessCompress(face2Verify);

                            }
                            catch (Exception ex)
                            {
                                claim.AgencyReport.DigitalIdReport.DigitalIdImageMatchConfidence = string.Empty;
                                claim.AgencyReport.DigitalIdReport.DigitalIdImage = CompressImage.ProcessCompress(face2Verify);
                                throw ex;
                            }
                        }
                        else
                        {
                            claim.AgencyReport.DigitalIdReport.DigitalIdImageMatchConfidence = "no face image";
                        }
                    }
                    catch (Exception ex)
                    {
                        claim.AgencyReport.DigitalIdReport.DigitalIdImageMatchConfidence = "err " + ImageData;
                    }
                    if (registeredImage == null)
                    {
                        claim.AgencyReport.DigitalIdReport.DigitalIdImageMatchConfidence = "no image";
                    }
                }

                #endregion FACE IMAGE PROCESSING

                if (!string.IsNullOrWhiteSpace(data.LocationLongLat))
                {
                    claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLatTime = DateTime.Now;
                    claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat = data.LocationLongLat;

                    var longLat = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.IndexOf("/");
                    var latitude = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.AgencyReport.DigitalIdReport.DigitalIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                    var latLongString = latitude + "," + longitude;
                    var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                    var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
                    string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                        $"\r\n" +
                        $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                        $"\r\n" +
                        $"\r\nElevation(sea level):{weatherData.elevation} metres";
                    claim.AgencyReport.DigitalIdReport.DigitalIdImageData = weatherCustomData;
                    var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                    claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationUrl = url;

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
                        registeredLatitude = Convert.ToDouble(claim.BeneficiaryDetail.PinCode.Latitude);
                        registeredLongitude = Convert.ToDouble(claim.BeneficiaryDetail.PinCode.Longitude);
                    }

                    var address = rootObject.display_name;

                    claim.AgencyReport.DigitalIdReport.DigitalIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;

                }


                claim.AgencyReport.DigitalIdReport.MatchExecuted = true;
                claim.AgencyReport.DigitalIdReport.Updated = DateTime.Now;
                claim.AgencyReport.DigitalIdReport.UpdatedBy = claim.AgencyReport.AgentEmail;

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
                throw ex;
            }
        }

        public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
        {
            try
            {
                var claim = claimsService.GetClaims()
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.DocumentIdReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);
                if (claim.AgencyReport == null)
                {
                    claim.AgencyReport = new AgencyReport();
                }
                claim.AgencyReport.AgentEmail = data.Email;

                var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.PolicyDetail.ClientCompanyId);

                #region PAN IMAGE PROCESSING

                if (!string.IsNullOrWhiteSpace(data.OcrImage))
                {
                    //=================GOOGLE VISION API =========================

                    var byteimage = Convert.FromBase64String(data.OcrImage);



                    var imageReadOnly = await googleApi.DetectTextAsync(byteimage);

                    var allPanText = imageReadOnly.FirstOrDefault().Description;

                    var panTextPre = allPanText.IndexOf(txt2Find);
                    if (panTextPre != -1)
                    {
                        var panNumber = allPanText.Substring(panTextPre + txt2Find.Length + 1, 10);


                        var ocrImaged = googleHelper.MaskTextInImage(byteimage, imageReadOnly);
                        var docyTypePan = allPanText.IndexOf(txt2Find) > 0 && allPanText.Length > allPanText.IndexOf(txt2Find) ? "PAN" : "UNKNOWN";
                        var maskedImage = new FaceImageDetail
                        {
                            DocType = docyTypePan,
                            DocumentId = panNumber,
                            MaskedImage = Convert.ToBase64String(ocrImaged),       //TO-DO,
                            OcrData = allPanText
                        };
                        try
                        {
                            #region// PAN VERIFICATION ::: //test PAN FNLPM8635N, BYSPP5796F
                            if (company.VerifyOcr)
                            {
                                try
                                {
                                    //var body = await httpClientService.VerifyPan(maskedImage.DocumentId, company.PanIdfyUrl, company.RapidAPIKey, company.RapidAPITaskId, company.RapidAPIGroupId);
                                    //company.RapidAPIPanRemainCount = body?.count_remain;

                                    //if (body != null && body?.status == "completed" &&
                                    //    body?.result != null &&
                                    //    body.result?.source_output != null
                                    //    && body.result?.source_output?.status == "id_found")
                                    var panMatch = panRegex.Match(maskedImage.DocumentId);
                                    if (panMatch.Success)
                                    {
                                        claim.AgencyReport.DocumentIdReport.DocumentIdImageValid = true;
                                    }
                                    else
                                    {
                                        claim.AgencyReport.DocumentIdReport.DocumentIdImageValid = false;
                                    }
                                }
                                catch (Exception)
                                {
                                    claim.AgencyReport.DocumentIdReport.DocumentIdImageValid = false;
                                }
                            }
                            else
                            {
                                claim.AgencyReport.DocumentIdReport.DocumentIdImageValid = true;
                            }

                            #endregion PAN IMAGE PROCESSING

                            var image = Convert.FromBase64String(maskedImage.MaskedImage);

                            var savedMaskedImage = CompressImage.ProcessCompress(image);

                            claim.AgencyReport.DocumentIdReport.DocumentIdImage = savedMaskedImage;

                            claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                            claim.AgencyReport.DocumentIdReport.DocumentIdImageType = maskedImage.DocType;
                            claim.AgencyReport.DocumentIdReport.DocumentIdImageData = maskedImage.DocType + " data: ";

                            if (!string.IsNullOrWhiteSpace(maskedImage.OcrData))
                            {
                                claim.AgencyReport.DocumentIdReport.DocumentIdImageData = maskedImage.DocType + " data:. \r\n " +
                                    "" + maskedImage.OcrData.Replace(maskedImage.DocumentId, "xxxxxxxxxx");
                            }
                        }
                        catch (Exception)
                        {
                            var image = Convert.FromBase64String(maskedImage.MaskedImage);

                            claim.AgencyReport.DocumentIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);

                            claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                            claim.AgencyReport.DocumentIdReport.DocumentIdImageData = "no data: ";
                        }
                    }
                    //=================END GOOGLE VISION  API =========================

                    else
                    {
                        var image = Convert.FromBase64String(data.OcrImage);

                        claim.AgencyReport.DocumentIdReport.DocumentIdImage = CompressImage.ProcessCompress(image);
                        claim.AgencyReport.DocumentIdReport.DocumentIdImageValid = false;
                        claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                        claim.AgencyReport.DocumentIdReport.DocumentIdImageData = "no data: ";
                    }
                }

                #endregion PAN IMAGE PROCESSING

                if (!string.IsNullOrWhiteSpace(data.OcrLongLat))
                {
                    claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat = data.OcrLongLat;
                    claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLatTime = DateTime.Now;
                    var longLat = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.IndexOf("/");
                    var latitude = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.Substring(0, longLat)?.Trim();
                    var longitude = claim.AgencyReport.DocumentIdReport.DocumentIdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
                    var latLongString = latitude + "," + longitude;
                    var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Applicationsettings.GMAPData}";
                    claim.AgencyReport.DocumentIdReport.DocumentIdImageLocationUrl = url;
                    RootObject rootObject = await httpClientService.GetAddress((latitude), (longitude));
                    
                    
                    double registeredLatitude = 0;
                    double registeredLongitude = 0;
                    if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
                    {
                        registeredLatitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Latitude);
                        registeredLongitude = Convert.ToDouble(claim.CustomerDetail.PinCode.Longitude);
                    }

                    var address = rootObject.display_name;

                    claim.AgencyReport.DocumentIdReport.DocumentIdImageLocationAddress = string.IsNullOrWhiteSpace(rootObject.display_name) ? "12 Heathcote Drive Forest Hill VIC 3131" : address;
                }
                claim.AgencyReport.DocumentIdReport.ValidationExecuted = true;
                claim.AgencyReport.DocumentIdReport.Updated = DateTime.Now;
                claim.AgencyReport.DocumentIdReport.UpdatedBy = claim.AgencyReport.AgentEmail;

                _context.ClaimsInvestigation.Update(claim);

                var rows = await _context.SaveChangesAsync();

                var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                var noDataimage = await File.ReadAllBytesAsync(noDataImagefilePath);
                return new AppiCheckifyResponse
                {
                    BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                    OcrImage = claim.AgencyReport.DocumentIdReport?.DocumentIdImage != null ?
                    Convert.ToBase64String(claim.AgencyReport.DocumentIdReport?.DocumentIdImage) :
                    Convert.ToBase64String(noDataimage),
                    OcrLongLat = claim.AgencyReport.DocumentIdReport?.DocumentIdImageLongLat,
                    OcrTime = claim.AgencyReport.DocumentIdReport?.DocumentIdImageLongLatTime,
                    PanValid = claim.AgencyReport.DocumentIdReport?.DocumentIdImageValid
                };
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public async Task GetAudio(AudioData data)
        {
            var claim = _context.ClaimsInvestigation
                .Include(c => c.AgencyReport)
                .Include(c => c.AgencyReport.ReportQuestionaire)
                .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            using var dataStream = new MemoryStream();
            data.Uri.CopyTo(dataStream);
            string audioDirectory = Path.Combine(webHostEnvironment.WebRootPath, "audio");
            if (!Directory.Exists(audioDirectory))
            {
                Directory.CreateDirectory(audioDirectory);
            }
            var audioPath = Path.Combine(audioDirectory, $"{Guid.NewGuid()}.mp3");
            File.WriteAllBytes(audioPath, dataStream.ToArray());
            claim.AgencyReport.ReportQuestionaire.Audio = dataStream.ToArray();
            claim.AgencyReport.ReportQuestionaire.AudioUrl = audioPath;
            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
        }

        public async Task GetVideo(VideoData data)
        {
            var claim = _context.ClaimsInvestigation
                            .Include(c => c.AgencyReport)
                            .Include(c => c.AgencyReport.ReportQuestionaire)
                            .FirstOrDefault(c => c.ClaimsInvestigationId == data.ClaimId);

            using var dataStream = new MemoryStream();
            data.Uri.CopyTo(dataStream);
            string videoDirectory = Path.Combine(webHostEnvironment.WebRootPath, "video");
            if (!Directory.Exists(videoDirectory))
            {
                Directory.CreateDirectory(videoDirectory);
            }
            var videoPath = Path.Combine(videoDirectory, $"{Guid.NewGuid()}.mp4");
            File.WriteAllBytes(videoPath, dataStream.ToArray());
            claim.AgencyReport.ReportQuestionaire.Video = dataStream.ToArray();
            claim.AgencyReport.ReportQuestionaire.VideoUrl = videoPath;
            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
        }
    }
}