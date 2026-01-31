using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services;

public interface IAgentIdfyService
{
    Task<AppiCheckifyResponse> CaptureFaceId(FaceData data);
}

internal class AgentIdfyService : IAgentIdfyService
{
    private readonly ApplicationDbContext context;
    private readonly ICaseService caseService;
    private readonly IWeatherInfoService weatherInfoService;
    private readonly ILogger<AgentIdfyService> logger;
    private readonly IFileStorageService fileStorageService;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiClient customApiCLient;
    private readonly IFaceMatchService faceMatchService;

    public AgentIdfyService(ApplicationDbContext context,
        ICaseService caseService,
        IWeatherInfoService weatherInfoService,
        ILogger<AgentIdfyService> logger,
        IFileStorageService fileStorageService,
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
        this.webHostEnvironment = webHostEnvironment;
        this.httpClientService = httpClientService;
        this.customApiCLient = customApiCLient;
        this.faceMatchService = faceMatchService;
    }

    public async Task<AppiCheckifyResponse> CaptureFaceId(FaceData data)
    {
        var claim = await caseService.GetCaseById(data.CaseId);
        if (claim?.InvestigationReport == null) return null;

        // 1. Resolve Entities
        var agent = await context.ApplicationUser.FirstAsync(u => u.Email == data.Email);
        var location = claim.InvestigationReport.ReportTemplate.LocationReport
            .First(l => l.LocationName == data.LocationName);

        var locationTemplate = await context.LocationReport
            .Include(l => l.FaceIds)
            .FirstAsync(l => l.Id == location.Id);

        var face = locationTemplate.FaceIds.First(c => c.ReportName == data.ReportName);

        try
        {
            // 2. Data Preparation
            bool isCustomer = face.ReportName == DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName();
            var (lat, lon) = FaceIdHelper.ParseCoordinates(data.LocationLatLong);
            var expected = FaceIdHelper.GetExpectedCoordinates(claim);

            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");
            var faceBytes = await GetBytesAsync(data.Image);

            var regPath = Path.Combine(webHostEnvironment.ContentRootPath, FaceIdHelper.GetRegisteredImagePath(claim, isCustomer));
            var registeredImage = await File.ReadAllBytesAsync(regPath);

            // 3. Parallel Execution (External APIs)
            var faceTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, Path.GetExtension(fileName));
            var weatherTask = weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = httpClientService.GetRawAddress(lat, lon);
            var mapTask = customApiCLient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon), "Start", "End", "300", "300", "green", "red");

            await Task.WhenAll(faceTask, weatherTask, addressTask, mapTask);

            // 4. Update and Save
            FaceIdHelper.MapMetadata(face, locationTemplate, data, relativePath, Path.GetExtension(fileName), lat, lon);

            var (conf, compImg, sim) = await faceTask;
            var (dist, distM, dur, durS, mapUrl) = await mapTask;

            face.LocationMapUrl = mapUrl;
            face.Distance = dist;
            face.Duration = dur;
            face.LocationInfo = await weatherTask;
            face.LocationAddress = await addressTask;
            face.MatchConfidence = conf;
            face.Similarity = sim;
            face.ImageValid = sim > 70;

            await File.WriteAllBytesAsync(face.FilePath, compImg);
            await context.SaveChangesAsync();

            return BuildResponse(claim, face, compImg);
        }
        catch (Exception ex)
        {
            return await HandleError(claim, face, ex);
        }
    }

    private async Task<byte[]> GetBytesAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }

    private AppiCheckifyResponse BuildResponse(InvestigationTask claim, FaceIdReport face, byte[] img)
    {
        return new AppiCheckifyResponse
        {
            BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
            Image = img,
            LocationImage = face.FilePath,
            LocationLongLat = face.LongLat,
            LocationTime = face.LongLatTime,
            FacePercent = face.MatchConfidence
        };
    }

    private async Task<AppiCheckifyResponse> HandleError(InvestigationTask claim, FaceIdReport face, Exception ex)
    {
        // Error logging and fallback logic
        face.LocationInfo = "No Data";
        face.ImageValid = false;
        await context.SaveChangesAsync();
        var img = File.Exists(face.FilePath) ? await File.ReadAllBytesAsync(face.FilePath) : null;
        return BuildResponse(claim, face, img);
    }
}
