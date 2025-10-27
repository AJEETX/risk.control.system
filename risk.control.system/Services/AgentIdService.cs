using Hangfire;

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
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly IPanCardService panCardService;
    private readonly IGoogleApi googleApi;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiCLient customApiCLient;
    private readonly IFaceMatchService faceMatchService;

    private static HttpClient httpClient = new();

    //test PAN FNLPM8635N
    public AgentIdService(ApplicationDbContext context,
        IBackgroundJobClient backgroundJobClient,
        IPanCardService panCardService,
        IGoogleApi googleApi,
        IWebHostEnvironment webHostEnvironment,
        IHttpClientService httpClientService,
        ICustomApiCLient customApiCLient,
        IFaceMatchService faceMatchService)
    {
        this._context = context;
        this.backgroundJobClient = backgroundJobClient;
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
        LocationReport location = null;
        string filePath = string.Empty;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
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

            location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationReport
                .Include(l => l.AgentIdReport)
                .FirstOrDefault(l => l.Id == location.Id);

            face = locationTemplate.AgentIdReport;

            string imageFileNameWithExtension = Path.GetFileName(data.Image.FileName.ToLower());
            string onlyExtension = Path.GetExtension(imageFileNameWithExtension);
            face.ImageExtension = onlyExtension;

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(data.Image.FileName);
            var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "agent-face");

            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            filePath = Path.Combine(webHostEnvironment.WebRootPath, "agent-face", fileName);
            byte[] faceBytes;
            using (var dataStream = new MemoryStream())
            {
                data.Image.CopyTo(dataStream);
                faceBytes = dataStream.ToArray();
                await File.WriteAllBytesAsync(filePath, faceBytes);
                face.FilePath = "/agent-face/" + fileName;
            }
            locationTemplate.Updated = DateTime.Now;
            locationTemplate.AgentEmail = agent.Email;
            locationTemplate.ValidationExecuted = true;
            _context.LocationReport.Update(locationTemplate);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.Now;
            face.LongLat = data.LocationLatLong;
            var longLat = face.LongLat.IndexOf("/");
            var latitude = face.LongLat.Substring(0, longLat)?.Trim();
            var longitude = face.LongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
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

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, onlyExtension);
            var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
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

            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}.\r\n" +
                $"Windspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}\r\n" +
                $"Elevation(sea level):{weatherData.elevation} metres";
            face.LocationAddress = $"{address}";
            face.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            face.ValidationExecuted = true;

            face.LocationInfo = weatherCustomData;
            var (confidence, compressImage, similarity) = await faceMatchTask;

            await File.WriteAllBytesAsync(filePath, compressImage);
            face.DigitalIdImageMatchConfidence = confidence;
            face.Similarity = similarity;
            face.ImageValid = similarity > 70;
            _context.AgentIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync(null, false);

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = compressImage,
                LocationImage = filePath,
                LocationLongLat = face.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            face.LocationInfo = "No Data";
            face.DigitalIdImageMatchConfidence = string.Empty;
            face.LocationAddress = "No Address data";
            face.ValidationExecuted = true;
            _context.AgentIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(filePath),
                LocationImage = (filePath),
                LocationLongLat = face?.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.DigitalIdImageMatchConfidence
            };
        }
    }

    public async Task<AppiCheckifyResponse> GetFaceId(FaceData data)
    {
        InvestigationTask claim = null;
        FaceIdReport face = null;
        LocationReport location = null;
        string filePath = string.Empty;
        try
        {
            claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
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

            location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationReport
                .Include(l => l.FaceIds)
                .FirstOrDefault(l => l.Id == location.Id);

            face = locationTemplate.FaceIds.FirstOrDefault(c => c.ReportName == data.ReportName);

            var hasCustomerVerification = face.ReportName == DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName();
            string imageFileNameWithExtension = Path.GetFileName(data.Image.FileName);
            string imageFileName = Path.GetFileNameWithoutExtension(imageFileNameWithExtension);
            string onlyExtension = Path.GetExtension(imageFileNameWithExtension);
            face.ImageExtension = onlyExtension;

            var extension = Path.GetExtension(data.Image.FileName).ToLower();
            var fileName = Guid.NewGuid().ToString() + extension;
            var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "face");

            // Ensure directory exists
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            // Full file path
            filePath = Path.Combine(webHostEnvironment.WebRootPath, "face", fileName);
            using var stream = new MemoryStream();
            await data.Image.CopyToAsync(stream);
            await File.WriteAllBytesAsync(filePath, stream.ToArray());

            face.FilePath = "/face/" + fileName;
            var faceBytes = stream.ToArray();

            locationTemplate.AgentEmail = agent.Email;
            locationTemplate.Updated = DateTime.Now;
            locationTemplate.ValidationExecuted = true;
            _context.LocationReport.Update(locationTemplate);
            face.Updated = DateTime.Now;
            face.UpdatedBy = data.Email;
            face.LongLatTime = DateTime.Now;
            face.LongLat = data.LocationLatLong;
            var longLat = face.LongLat.IndexOf("/");
            var latitude = face.LongLat.Substring(0, longLat)?.Trim();
            var longitude = face.LongLat.Substring(longLat + 1)?.Trim().Replace("/", "").Trim();
            var latLongString = latitude + "," + longitude;
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            byte[]? registeredImage = null;

            if (!hasCustomerVerification)
            {
                var beneficiaryFilePath = claim.BeneficiaryDetail.ImagePath.Substring(1);
                var image = Path.Combine(webHostEnvironment.WebRootPath, beneficiaryFilePath);
                registeredImage = await File.ReadAllBytesAsync(image);
            }
            else
            {
                var customerFilePath = claim.CustomerDetail.ImagePath.Substring(1);
                var image = Path.Combine(webHostEnvironment.WebRootPath, customerFilePath);
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

            var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");

            #region FACE IMAGE PROCESSING

            var faceMatchTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, onlyExtension);
            var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
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

            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";

            face.LocationInfo = weatherCustomData;
            face.LocationAddress = $" {address}";
            face.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            face.ValidationExecuted = true;

            var (confidence, compressImage, similarity) = await faceMatchTask;

            await File.WriteAllBytesAsync(filePath, compressImage);
            //face.IdImage = compressImage;
            face.MatchConfidence = confidence;
            face.Similarity = similarity;
            face.ImageValid = similarity > 70;
            _context.DigitalIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync(null, false);

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(filePath),
                LocationImage = (filePath),
                LocationLongLat = face.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.MatchConfidence
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            face.LocationInfo = "No Data";
            face.MatchConfidence = string.Empty;
            face.LocationAddress = "No Address data";
            face.ValidationExecuted = true;
            face.ImageValid = false;
            _context.DigitalIdReport.Update(face);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();
            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(filePath),
                LocationImage = (filePath),
                LocationLongLat = face?.LongLat,
                LocationTime = face?.LongLatTime,
                FacePercent = face?.MatchConfidence
            };
        }
    }

    public async Task<AppiCheckifyResponse> GetDocumentId(DocumentData data)
    {
        InvestigationTask claim = null;
        DocumentIdReport doc = null;
        Task<string> addressTask = null;
        string filePath = string.Empty;
        try
        {
            claim = await _context.Investigations
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
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

            var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationReport
                .Include(l => l.DocumentIds)
                .FirstOrDefault(l => l.Id == location.Id);

            doc = locationTemplate.DocumentIds.FirstOrDefault(c => c.ReportName == data.ReportName);
            string imageFileNameWithExtension = Path.GetFileName(data.Image.FileName);
            string imageFileName = Path.GetFileNameWithoutExtension(imageFileNameWithExtension);
            string onlyExtension = Path.GetExtension(imageFileNameWithExtension);
            doc.ImageExtension = onlyExtension;

            var extension = Path.GetExtension(data.Image.FileName).ToLower();
            var fileName = Guid.NewGuid().ToString() + extension;
            var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "document");

            // Ensure directory exists
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            // Full file path
            filePath = Path.Combine(webHostEnvironment.WebRootPath, "document", fileName);

            using var stream = new MemoryStream();
            await data.Image.CopyToAsync(stream);
            byte[] docImage = stream.ToArray();
            await File.WriteAllBytesAsync(filePath, docImage);
            doc.FilePath = "/document/" + fileName;

            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.Now;
            _context.LocationReport.Update(locationTemplate);
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
            var mapTask = customApiCLient.GetMap(double.Parse(expectedLat), double.Parse(expectedLong), double.Parse(latitude), double.Parse(longitude), "A", "X", "300", "300", "green", "red");

            var googleDetecTask = googleApi.DetectTextAsync(filePath);

            addressTask = httpClientService.GetRawAddress(latitude, longitude);

            await Task.WhenAll(googleDetecTask, addressTask, mapTask);

            var (distance, distanceInMetres, duration, durationInSecs, map) = await mapTask;
            doc.DistanceInMetres = distanceInMetres;
            doc.DurationInSeconds = durationInSecs;
            doc.Duration = duration;
            doc.Distance = distance;
            doc.LocationMapUrl = map;

            var company = _context.ClientCompany.FirstOrDefault(c => c.ClientCompanyId == claim.ClientCompanyId);
            var imageReadOnly = await googleDetecTask;
            if (imageReadOnly != null && imageReadOnly.Count > 0)
            {
                //PAN
                if (doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName())
                {
                    doc = await panCardService.Process(docImage, imageReadOnly, company, doc, onlyExtension);
                }
                else
                {
                    await File.WriteAllBytesAsync(filePath, CompressImage.ProcessCompress(docImage, onlyExtension));
                    //doc.IdImage = CompressImage.ProcessCompress(doc.IdImage, onlyExtension);
                    doc.ImageValid = true;
                    doc.LongLatTime = DateTime.Now;
                    var allText = imageReadOnly.FirstOrDefault().Description;
                    doc.LocationInfo = allText;
                }
            }
            else
            {
                await File.WriteAllBytesAsync(filePath, CompressImage.ProcessCompress(docImage, onlyExtension));
                //doc.IdImage = CompressImage.ProcessCompress(doc.IdImage, onlyExtension);
                doc.ImageValid = false;
                doc.LongLatTime = DateTime.Now;
                doc.LocationInfo = "no data: ";
            }

            var rawAddress = await addressTask;
            doc.LocationAddress = $"{rawAddress}";
            doc.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            doc.ValidationExecuted = true;
            _context.DocumentIdReport.Update(doc);
            _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync(null, false);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
                Image = docImage,
                OcrImage = (filePath),
                OcrLongLat = doc?.LongLat,
                OcrTime = doc?.LongLatTime,
                Valid = doc?.ImageValid
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            doc.LocationInfo = "No Data";
            doc.ImageValid = false;
            doc.LocationAddress = "No Address data";
            doc.ValidationExecuted = true;
            _context.DocumentIdReport.Update(doc);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();

            return new AppiCheckifyResponse
            {
                BeneficiaryId = updateClaim.Entity.BeneficiaryDetail.BeneficiaryDetailId,
                Image = await File.ReadAllBytesAsync(filePath),
                LocationImage = (filePath),
                LocationLongLat = doc?.LongLat,
                LocationTime = doc?.LongLatTime,
                Valid = doc?.ImageValid
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
                 .Include(c => c.PolicyDetail)
                 .Include(c => c.CustomerDetail)
                 .Include(c => c.BeneficiaryDetail)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
                .FirstOrDefaultAsync(c => c.Id == data.CaseId);

            var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

            var locationTemplate = _context.LocationReport
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

            var weatherTask = httpClient.GetFromJsonAsync<Weather>(weatherUrl);
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
            media.FilePath = "/media/" + fileName;

            string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";

            media.ImageExtension = extension;
            media.MediaExtension = extension.TrimStart('.');
            media.ValidationExecuted = true;
            media.ImageValid = true;
            media.LocationAddress = $"{address}";
            media.LocationInfo = weatherCustomData;
            media.LongLat = $"Latitude = {latitude}, Longitude = {longitude}";
            media.LongLatTime = DateTime.UtcNow;
            var mimeType = data.Image.ContentType.ToLower();

            string[] videoExtensions = { ".mp4", ".webm", ".avi", ".mov", ".mkv" };
            bool isVideo = mimeType.StartsWith("video/") || videoExtensions.Contains(extension);

            media.MediaType = isVideo ? MediaType.VIDEO : MediaType.AUDIO;

            await _context.SaveChangesAsync(null, false);

            backgroundJobClient.Enqueue(() => httpClientService.TranscribeAsync(location.Id, data.ReportName, "media", fileName, filePath));

            return new AppiCheckifyResponse
            {
                Image = fileBytes,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            media.LocationInfo = "No Data";
            media.ImageValid = false;
            media.LocationAddress = "No Address data";
            media.ValidationExecuted = true;
            _context.MediaReport.Update(media);
            var updateClaim = _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync();

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

    public async Task<bool> Answers(string locationName, long caseId, List<QuestionTemplate> Questions)
    {
        var claim = await _context.Investigations
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.ReportTemplate)
                .ThenInclude(c => c.LocationReport)
                .FirstOrDefaultAsync(c => c.Id == caseId);

        var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == locationName);

        var locationTemplate = _context.LocationReport
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
        _context.LocationReport.Update(locationTemplate);
        var rowsAffected = await _context.SaveChangesAsync(null, false);
        return rowsAffected > 0;
    }
}