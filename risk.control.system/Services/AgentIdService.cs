using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services;

public interface IAgentIdService
{
    Task<AppiCheckifyResponse> GetAgentId(FaceData data);
    Task<AppiCheckifyResponse> GetFaceId(FaceData data);
    Task<AppiCheckifyResponse> GetDocumentId(DocumentData data);
    Task<AppiCheckifyResponse> GetMedia(DocumentData data);
    Task<bool> Answers(string locationName, long caseId, List<QuestionTemplate> Questions);
}

public class AgentIdService : IAgentIdService
{
    private readonly ApplicationDbContext _context;
    private readonly IPanCardService panCardService;
    private readonly IGoogleApi googleApi;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiCLient customApiCLient;
    private readonly IFaceMatchService faceMatchService;

    private static HttpClient httpClient = new();

    //test PAN FNLPM8635N
    public AgentIdService(ApplicationDbContext context,
        IPanCardService panCardService,
        IGoogleApi googleApi,
        IWebHostEnvironment webHostEnvironment,
        IHttpClientService httpClientService,
        ICustomApiCLient customApiCLient,
        IFaceMatchService faceMatchService)
    {
        this._context = context;
        this.panCardService = panCardService;
        this.googleApi = googleApi;
        this.webHostEnvironment = webHostEnvironment;
        this.httpClientService = httpClientService;
        this.customApiCLient = customApiCLient;
        this.faceMatchService = faceMatchService;
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
                .FirstOrDefault(l => l.Id == location.Id);


            face = locationTemplate.AgentIdReport;

            string imageFileNameWithExtension = Path.GetFileName(data.Image.FileName);
            string imageFileName = Path.GetFileNameWithoutExtension(imageFileNameWithExtension);
            string onlyExtension = Path.GetExtension(imageFileNameWithExtension);
            face.IdImageExtension = onlyExtension;
            using (var stream = new MemoryStream())
            {
                await data.Image.CopyToAsync(stream);
                face.IdImage = stream.ToArray();
            }

            locationTemplate.Updated = DateTime.Now;
            locationTemplate.AgentEmail = agent.Email;
            locationTemplate.ValidationExecuted = true;
            _context.LocationTemplate.Update(locationTemplate);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.IdImageLongLatTime = DateTime.Now;
            face.IdImageLongLat = data.LocationLatLong;
            var longLat = face.IdImageLongLat.IndexOf("/");
            var latitude = face.IdImageLongLat.Substring(0, longLat)?.Trim();
            var longitude = face.IdImageLongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
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

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, face.IdImage, onlyExtension);
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
            face.IdImageData = "No Data";
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
                .Include(l => l.FaceIds)
                .FirstOrDefault(l => l.Id == location.Id);

            face = locationTemplate.FaceIds.FirstOrDefault(c => c.ReportName == data.ReportName);

            var hasCustomerVerification = face.ReportName == DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName();
            string imageFileNameWithExtension = Path.GetFileName(data.Image.FileName);
            string imageFileName = Path.GetFileNameWithoutExtension(imageFileNameWithExtension);
            string onlyExtension = Path.GetExtension(imageFileNameWithExtension);
            face.IdImageExtension = onlyExtension;
            using (var stream = new MemoryStream())
            {
                await data.Image.CopyToAsync(stream);
                face.IdImage = stream.ToArray();
            }

            locationTemplate.AgentEmail = agent.Email;
            locationTemplate.Updated = DateTime.Now;
            locationTemplate.ValidationExecuted = true;
            _context.LocationTemplate.Update(locationTemplate);
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

            if (!hasCustomerVerification)
            {
                registeredImage = claim.BeneficiaryDetail.ProfilePicture;
            }
            else
            {
                registeredImage = claim.CustomerDetail.ProfilePicture;
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

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, face.IdImage, onlyExtension);
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
            face.MatchConfidence = confidence;
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
                FacePercent = face?.MatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            face.IdImageData = "No Data";
            face.MatchConfidence = string.Empty;
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
                LocationImage = Convert.ToBase64String(face?.IdImage),
                LocationLongLat = face?.IdImageLongLat,
                LocationTime = face?.IdImageLongLatTime,
                FacePercent = face?.MatchConfidence
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
                .Include(l => l.DocumentIds)
                .FirstOrDefault(l => l.Id == location.Id);

            doc = locationTemplate.DocumentIds.FirstOrDefault(c => c.ReportName == data.ReportName);
            string imageFileNameWithExtension = Path.GetFileName(data.Image.FileName);
            string imageFileName = Path.GetFileNameWithoutExtension(imageFileNameWithExtension);
            string onlyExtension = Path.GetExtension(imageFileNameWithExtension);
            doc.IdImageExtension = onlyExtension;
            using (var stream = new MemoryStream())
            {
                await data.Image.CopyToAsync(stream);
                doc.IdImage = stream.ToArray();
            }

            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.Now;
            _context.LocationTemplate.Update(locationTemplate);
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
                //PAN
                if (doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName())
                {
                    doc = await panCardService.Process(doc.IdImage, imageReadOnly, company, doc, onlyExtension);
                }
                else
                {
                    doc.IdImage = CompressImage.ProcessCompress(doc.IdImage, onlyExtension);
                    doc.IdImageValid = true;
                    doc.IdImageLongLatTime = DateTime.Now;
                    var allText = imageReadOnly.FirstOrDefault().Description;
                    doc.IdImageData = allText;
                }

            }

            else
            {
                doc.IdImage = CompressImage.ProcessCompress(doc.IdImage, onlyExtension);
                doc.IdImageValid = false;
                doc.IdImageLongLatTime = DateTime.Now;
                doc.IdImageData = "no data: ";
            }

            var rawAddress = await addressTask;
            doc.IdImageLocationAddress = rawAddress;
            doc.ValidationExecuted = true;

            _context.DocumentIdReport.Update(doc);
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
            doc.IdImageData = "No Data";
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

    public async Task<AppiCheckifyResponse> GetMedia(DocumentData data)
    {
        InvestigationTask claim = null;
        MediaReport media = null;
        Task<string> addressTask = null;
        byte[] fileBytes = null; ;
        try
        {
            var agent = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == data.Email);

            using var memoryStream = new MemoryStream();
            await data.Image.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
            var extension = Path.GetExtension(data.Image.FileName).ToLower();
            var fileName = Guid.NewGuid().ToString() + extension;
            var mediaPath = Path.Combine(webHostEnvironment.WebRootPath, "media");

            // Ensure directory exists
            if (!Directory.Exists(mediaPath))
            {
                Directory.CreateDirectory(mediaPath);
            }

            // Full file path
            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "media", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await data.Image.CopyToAsync(stream);
            }

            claim = await _context.Investigations
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationTemplate)
                .FirstOrDefaultAsync(c => c.Id == data.CaseId);

            var location = claim.InvestigationReport.ReportTemplate.LocationTemplate.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationTemplate
                .Include(l => l.MediaReports)
                .FirstOrDefault(l => l.Id == location.Id);

            // Save to DB
            media = locationTemplate.MediaReports.FirstOrDefault(c => c.ReportName == data.ReportName);

            var longLat = data.LocationLatLong.IndexOf("/");
            var latitude = data.LocationLatLong.Substring(0, longLat)?.Trim();
            var longitude = data.LocationLatLong.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={latLongString}&zoom=14&size=200x200&maptype=roadmap&markers=color:red%7Clabel:S%7C{latLongString}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";

            var mapTask = customApiCLient.GetMap(double.Parse(latitude), double.Parse(longitude), double.Parse(agent.AddressLatitude), double.Parse(agent.AddressLongitude), "A", "X", "300", "300", "green", "red");

            var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
            addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(addressTask, weatherTask, mapTask);

            var address = await addressTask;
            var weatherData = await weatherTask;
            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;


            media.IdImageLocationUrl = map;
            media.Duration = duration;
            media.Distance = distance;
            media.DistanceInMetres = distanceInMetres;
            media.DurationInSeconds = durationInSecs;
            media.FilePath = "/media/" + fileName;

            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";


            media.IdImageExtension = extension;
            media.MediaExtension = extension.TrimStart('.');
            media.ValidationExecuted = true;
            media.IdImageValid = true;
            media.IdImageLocationAddress = address;
            media.IdImageData = weatherCustomData;
            media.IdImageLongLat = $"{latitude}/{longitude}";
            media.IdImageLongLatTime = DateTime.UtcNow;
            var mimeType = data.Image.ContentType.ToLower();

            string[] videoExtensions = { ".mp4", ".webm", ".avi", ".mov", ".mkv" };
            bool isVideo = mimeType.StartsWith("video/") || videoExtensions.Contains(extension);

            media.MediaType = isVideo ? MediaType.VIDEO : MediaType.AUDIO;

            await _context.SaveChangesAsync();

            return new AppiCheckifyResponse
            {
                Image = fileBytes,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            media.IdImageData = "No Data";
            media.IdImageValid = false;
            media.IdImageLocationAddress = "No Address data";
            media.ValidationExecuted = true;
            _context.MediaReport.Update(media);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = fileBytes,
                LocationImage = Convert.ToBase64String(fileBytes),
                LocationLongLat = media?.IdImageLongLat,
                LocationTime = media?.IdImageLongLatTime,
                Valid = media?.IdImageValid
            };
        }
    }

    public async Task<bool> Answers(string locationName, long caseId, List<QuestionTemplate> Questions)
    {
        var claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationTemplate)
                .FirstOrDefaultAsync(c => c.Id == caseId);

        var location = claim.InvestigationReport.ReportTemplate.LocationTemplate.FirstOrDefault(l => l.LocationName == locationName);

        var locationTemplate = _context.LocationTemplate
            .Include(l => l.Questions)
            .FirstOrDefault(l => l.Id == location.Id);

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
        _context.LocationTemplate.Update(locationTemplate);
        var rowsAffected = await _context.SaveChangesAsync();
        return rowsAffected > 0;
    }
}