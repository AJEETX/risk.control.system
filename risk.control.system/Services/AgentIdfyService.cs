using Hangfire;

using Microsoft.EntityFrameworkCore;

using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services;

public interface IAgentIdfyService
{
    Task<AppiCheckifyResponse> CaptureAgentId(FaceData data);
    Task<AppiCheckifyResponse> CaptureFaceId(FaceData data);
    Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data);
    Task<AppiCheckifyResponse> CaptureMedia(DocumentData data);
    Task<bool> CaptureAnswers(string locationName, long caseId, List<QuestionTemplate> Questions);
}

internal class AgentIdfyService : IAgentIdfyService
{
    private readonly ApplicationDbContext context;
    private readonly ICaseService caseService;
    private readonly IWeatherInfoService weatherInfoService;
    private readonly ILogger<AgentIdfyService> logger;
    private readonly IFileStorageService fileStorageService;
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly IPanCardService panCardService;
    private readonly IGoogleApi googleApi;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiClient customApiCLient;
    private readonly IFaceMatchService faceMatchService;

    public AgentIdfyService(ApplicationDbContext context,
        ICaseService caseService,
        IWeatherInfoService weatherInfoService,
        ILogger<AgentIdfyService> logger,
        IFileStorageService fileStorageService,
        IBackgroundJobClient backgroundJobClient,
        IPanCardService panCardService,
        IGoogleApi googleApi,
        IWebHostEnvironment webHostEnvironment,
        IHttpClientService httpClientService,
        ICustomApiClient customApiCLient,
        IFaceMatchService faceMatchService)
    {
        this.context = context;
        this.caseService = caseService;
        this.weatherInfoService = weatherInfoService;
        this.logger = logger;
        this.fileStorageService = fileStorageService;
        this.backgroundJobClient = backgroundJobClient;
        this.panCardService = panCardService;
        this.googleApi = googleApi;
        this.webHostEnvironment = webHostEnvironment;
        this.httpClientService = httpClientService;
        this.customApiCLient = customApiCLient;
        this.faceMatchService = faceMatchService;
    }

    public async Task<AppiCheckifyResponse> CaptureAgentId(FaceData data)
    {
        InvestigationTask claim = null;
        AgentIdReport face = null;
        LocationReport location = null;
        byte[] faceBytes;
        try
        {
            claim = await caseService.GetCaseById(data.CaseId);

            if (claim.InvestigationReport == null)
            {
                return null;
            }
            var agent = await context.VendorApplicationUser.FirstOrDefaultAsync(u => u.Email == data.Email);

            location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = await context.LocationReport
                .Include(l => l.AgentIdReport)
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            face = locationTemplate.AgentIdReport;
            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");
            face.FilePath = relativePath;
            face.ImageExtension = Path.GetExtension(fileName);
            using (var dataStream = new MemoryStream())
            {
                data.Image.CopyTo(dataStream);
                faceBytes = dataStream.ToArray();
            }
            locationTemplate.Updated = DateTime.Now;
            locationTemplate.AgentEmail = agent.Email;
            locationTemplate.ValidationExecuted = true;
            context.LocationReport.Update(locationTemplate);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.Now;
            face.LongLat = data.LocationLatLong;
            var longLat = face.LongLat.IndexOf("/");
            var latitude = face.LongLat.Substring(0, longLat)?.Trim();
            var longitude = face.LongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
                var registeredImage = await System.IO.File.ReadAllBytesAsync(Path.Combine(webHostEnvironment.ContentRootPath, agent.ProfilePictureUrl));

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

            var mapTask = customApiCLient.GetMap(expectedLat, expectedLong, latitude, longitude, "A", "X", "300", "300", "green", "red");

            #region FACE IMAGE PROCESSING

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, face.ImageExtension);
            var weatherTask = weatherInfoService.GetWeatherAsync(latitude, longitude);
            var addressTask = httpClientService.GetRawAddress(latitude, longitude);
            #endregion FACE IMAGE PROCESSING

            await Task.WhenAll(faceMatchTask, addressTask, weatherTask, mapTask);

            var address = await addressTask;
            var weatherData = await weatherTask;
            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;

            face.LocationMapUrl = map;
            face.Duration = duration;
            face.Distance = distance;
            face.DistanceInMetres = distanceInMetres;
            face.DurationInSeconds = durationInSecs;

            face.LocationAddress = $"{address}";
            face.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            face.ValidationExecuted = true;

            face.LocationInfo = weatherData;
            var (confidence, compressImage, similarity) = await faceMatchTask;

            await File.WriteAllBytesAsync(face.FilePath, compressImage);
            face.DigitalIdImageMatchConfidence = confidence;
            face.Similarity = similarity;
            face.ImageValid = similarity > 70;
            context.AgentIdReport.Update(face);
            var updateClaim = context.Investigations.Update(claim);
            var rows = await context.SaveChangesAsync(null, false);

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = compressImage,
                LocationImage = face.FilePath,
                LocationLongLat = face.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed agent Id match");
            face.LocationInfo = "No Data";
            face.DigitalIdImageMatchConfidence = string.Empty;
            face.LocationAddress = "No Address data";
            face.ValidationExecuted = true;
            context.AgentIdReport.Update(face);
            var updateClaim = context.Investigations.Update(claim);
            var rows = await context.SaveChangesAsync();
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(face.FilePath),
                LocationImage = (face.FilePath),
                LocationLongLat = face?.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
    }

    public async Task<AppiCheckifyResponse> CaptureFaceId(FaceData data)
    {
        InvestigationTask claim = null;
        FaceIdReport face = null;
        LocationReport location = null;
        try
        {
            claim = await caseService.GetCaseById(data.CaseId);

            if (claim.InvestigationReport == null)
            {
                return null;
            }
            var agent = context.VendorApplicationUser.FirstOrDefault(u => u.Email == data.Email);

            location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = context.LocationReport
                .Include(l => l.FaceIds)
                .FirstOrDefault(l => l.Id == location.Id);

            face = locationTemplate.FaceIds.FirstOrDefault(c => c.ReportName == data.ReportName);

            var hasCustomerVerification = face.ReportName == DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName();
            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");
            face.FilePath = relativePath;
            face.ImageExtension = Path.GetExtension(fileName);

            using var stream = new MemoryStream();
            await data.Image.CopyToAsync(stream);
            var faceBytes = stream.ToArray();

            locationTemplate.AgentEmail = agent.Email;
            locationTemplate.Updated = DateTime.Now;
            locationTemplate.ValidationExecuted = true;
            context.LocationReport.Update(locationTemplate);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.Now;
            face.LongLat = data.LocationLatLong;
            var longLat = face.LongLat.IndexOf("/");
            var latitude = face.LongLat.Substring(0, longLat)?.Trim();
            var longitude = face.LongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            byte[]? registeredImage = null;

            if (!hasCustomerVerification)
            {
                var image = Path.Combine(webHostEnvironment.ContentRootPath, claim.BeneficiaryDetail.ImagePath);
                registeredImage = await File.ReadAllBytesAsync(image);
            }
            else
            {
                var image = Path.Combine(webHostEnvironment.ContentRootPath, claim.CustomerDetail.ImagePath);
                registeredImage = await File.ReadAllBytesAsync(image);
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

            var mapTask = customApiCLient.GetMap(expectedLat, expectedLong, latitude, longitude, "A", "X", "300", "300", "green", "red");

            #region FACE IMAGE PROCESSING

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, face.ImageExtension);
            var weatherTask = weatherInfoService.GetWeatherAsync(latitude, longitude);
            var addressTask = httpClientService.GetRawAddress(latitude, longitude);
            #endregion FACE IMAGE PROCESSING

            await Task.WhenAll(faceMatchTask, addressTask, weatherTask, mapTask);

            var address = await addressTask;
            var weatherData = await weatherTask;
            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;

            face.LocationMapUrl = map;
            face.Duration = duration;
            face.Distance = distance;
            face.DistanceInMetres = distanceInMetres;
            face.DurationInSeconds = durationInSecs;
            face.LocationInfo = weatherData;
            face.LocationAddress = $" {address}";
            face.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            face.ValidationExecuted = true;

            var (confidence, compressImage, similarity) = await faceMatchTask;

            await File.WriteAllBytesAsync(face.FilePath, compressImage);
            //face.IdImage = compressImage;
            face.MatchConfidence = confidence;
            face.Similarity = similarity;
            face.ImageValid = similarity > 70;
            context.DigitalIdReport.Update(face);
            var updateClaim = context.Investigations.Update(claim);
            var rows = await context.SaveChangesAsync(null, false);

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(face.FilePath),
                LocationImage = (face.FilePath),
                LocationLongLat = face.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.MatchConfidence
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed gace Id match");
            face.LocationInfo = "No Data";
            face.MatchConfidence = string.Empty;
            face.LocationAddress = "No Address data";
            face.ValidationExecuted = true;
            face.ImageValid = false;
            context.DigitalIdReport.Update(face);
            var updateClaim = context.Investigations.Update(claim);
            var rows = await context.SaveChangesAsync();
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(face.FilePath),
                LocationImage = (face.FilePath),
                LocationLongLat = face?.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.MatchConfidence
            };
        }
    }

    public async Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data)
    {
        InvestigationTask claim = null;
        DocumentIdReport doc = null;
        Task<string> addressTask = null;
        try
        {
            claim = await caseService.GetCaseById(data.CaseId);

            var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = context.LocationReport
                .Include(l => l.DocumentIds)
                .FirstOrDefault(l => l.Id == location.Id);

            doc = locationTemplate.DocumentIds.FirstOrDefault(c => c.ReportName == data.ReportName);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");
            doc.FilePath = relativePath;
            doc.ImageExtension = Path.GetExtension(fileName);

            using var stream = new MemoryStream();
            await data.Image.CopyToAsync(stream);
            byte[] docImage = stream.ToArray();


            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.Now;
            context.LocationReport.Update(locationTemplate);
            doc.LongLat = data.LocationLatLong;
            doc.LongLatTime = DateTime.Now;
            var longLat = doc.LongLat.IndexOf("/");
            var latitude = doc.LongLat.Substring(0, longLat)?.Trim();
            var longitude = doc.LongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
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
            var mapTask = customApiCLient.GetMap(expectedLat, expectedLong, latitude, longitude, "A", "X", "300", "300", "green", "red");

            var googleDetecTask = googleApi.DetectTextAsync(doc.FilePath);

            addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(googleDetecTask, addressTask, mapTask);

            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;
            doc.DistanceInMetres = distanceInMetres;
            doc.DurationInSeconds = durationInSecs;
            doc.Duration = duration;
            doc.Distance = distance;
            doc.LocationMapUrl = map;

            var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == claim.ClientCompanyId);
            var imageReadOnly = await googleDetecTask;
            if (imageReadOnly != null && imageReadOnly.Count > 0)
            {
                //PAN
                if (doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName())
                {
                    doc = await panCardService.Process(docImage, imageReadOnly, company, doc, doc.ImageExtension);
                }
                else
                {
                    await File.WriteAllBytesAsync(doc.FilePath, CompressImage.ProcessCompress(docImage, doc.ImageExtension));
                    //doc.IdImage = CompressImage.ProcessCompress(doc.IdImage, onlyExtension);
                    doc.ImageValid = true;
                    doc.LongLatTime = DateTime.Now;
                    var allText = imageReadOnly.FirstOrDefault().Description;
                    doc.LocationInfo = allText;
                }
            }
            else
            {
                await File.WriteAllBytesAsync(doc.FilePath, CompressImage.ProcessCompress(docImage, doc.ImageExtension));
                //doc.IdImage = CompressImage.ProcessCompress(doc.IdImage, onlyExtension);
                doc.ImageValid = false;
                doc.LongLatTime = DateTime.Now;
                doc.LocationInfo = "no data: ";
            }

            var rawAddress = await addressTask;
            doc.LocationAddress = $"{rawAddress}";
            doc.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            doc.ValidationExecuted = true;
            context.DocumentIdReport.Update(doc);
            context.Investigations.Update(claim);
            var rows = await context.SaveChangesAsync(null, false);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                Image = docImage,
                OcrImage = (doc.FilePath),
                OcrLongLat = doc?.LongLat,
                OcrTime = doc?.LongLatTime,
                Valid = doc?.ImageValid
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed face Id match");
            doc.LocationInfo = "No Data";
            doc.ImageValid = false;
            doc.LocationAddress = "No Address data";
            doc.ValidationExecuted = true;
            context.DocumentIdReport.Update(doc);
            var updateClaim = context.Investigations.Update(claim);
            var rows = await context.SaveChangesAsync();

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(doc.FilePath),
                LocationImage = (doc.FilePath),
                LocationLongLat = doc?.LongLat,
                LocationTime = doc?.LongLatTime,
                Valid = doc?.ImageValid
            };
        }
    }

    public async Task<AppiCheckifyResponse> CaptureMedia(DocumentData data)
    {
        InvestigationTask claim = null;
        MediaReport media = null;
        Task<string> addressTask = null;
        byte[] fileBytes = null; ;
        try
        {
            claim = await caseService.GetCaseByIdForMedia(data.CaseId);

            var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = await context.LocationReport
                .Include(l => l.MediaReports)
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            // Save to DB
            media = locationTemplate.MediaReports.FirstOrDefault(c => c.ReportName == data.ReportName);

            using var memoryStream = new MemoryStream();
            await data.Image.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
            var (fileName, relativePath) = await fileStorageService.SaveMediaAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");
            media.FilePath = relativePath;
            media.ImageExtension = Path.GetExtension(fileName);

            var longLat = data.LocationLatLong.IndexOf("/");
            var latitude = data.LocationLatLong.Substring(0, longLat)?.Trim();
            var longitude = data.LocationLatLong.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
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
            var mapTask = customApiCLient.GetMap(expectedLat, expectedLong, latitude, longitude, "A", "X", "300", "300", "green", "red");

            var weatherTask = weatherInfoService.GetWeatherAsync(latitude, longitude);
            addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(addressTask, weatherTask, mapTask);

            var address = await addressTask;
            var weatherData = await weatherTask;
            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;

            media.LocationMapUrl = map;
            media.Duration = duration;
            media.Distance = distance;
            media.DistanceInMetres = distanceInMetres;
            media.DurationInSeconds = durationInSecs;

            media.MediaExtension = media.ImageExtension.TrimStart('.');
            media.ValidationExecuted = true;
            media.ImageValid = true;
            media.LocationAddress = $"{address}";
            media.LocationInfo = weatherData;
            media.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            media.LongLatTime = DateTime.UtcNow;
            var mimeType = data.Image.ContentType.ToLower();

            string[] videoExtensions = { ".mp4", ".webm", ".avi", ".mov", ".mkv" };
            bool isVideo = mimeType.StartsWith("video/") || videoExtensions.Contains(media.ImageExtension);

            media.MediaType = isVideo ? MediaType.VIDEO : MediaType.AUDIO;

            await context.SaveChangesAsync(null, false);

            //backgroundJobClient.Enqueue(() => httpClientService.TranscribeAsync(location.Id, data.ReportName, "media", fileName, filePath));

            return new AppiCheckifyResponse
            {
                Image = fileBytes,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed media file capture");
            media.LocationInfo = "No Data";
            media.ImageValid = false;
            media.LocationAddress = "No Address data";
            media.ValidationExecuted = true;
            context.MediaReport.Update(media);
            var updateClaim = context.Investigations.Update(claim);
            var rows = await context.SaveChangesAsync();

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = fileBytes,
                LocationImage = Convert.ToBase64String(fileBytes),
                LocationLongLat = media?.LongLat,
                LocationTime = media?.LongLatTime,
                Valid = media?.ImageValid
            };
        }
    }

    public async Task<bool> CaptureAnswers(string locationName, long caseId, List<QuestionTemplate> Questions)
    {
        try
        {
            var claim = await caseService.GetCaseByIdForQuestions(caseId);

            var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == locationName);

            var locationTemplate = await context.LocationReport
                .Include(l => l.Questions)
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            locationTemplate.Questions.RemoveAll(q => true);
            foreach (var q in Questions)
            {
                locationTemplate.Questions.Add(new Question
                {
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    IsRequired = q.IsRequired,
                    Options = q.Options?.Trim(),
                    AnswerText = q.AnswerText?.Trim(),
                    Updated = DateTime.Now,
                });
            }
            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.Now;
            context.LocationReport.Update(locationTemplate);
            var rowsAffected = await context.SaveChangesAsync(null, false);
            return rowsAffected > 0;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed media file capture");
            return false;
        }
    }
}