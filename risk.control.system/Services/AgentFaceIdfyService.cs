using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services;

public interface IAgentFaceIdfyService
{
    Task<AppiCheckifyResponse> CaptureAgentId(FaceData data);
}

internal class AgentFaceIdfyService : IAgentFaceIdfyService
{
    private readonly ApplicationDbContext context;
    private readonly ICaseService caseService;
    private readonly IWeatherInfoService weatherInfoService;
    private readonly ILogger<AgentIdfyService> logger;
    private readonly IFileStorageService fileStorageService;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiClient customApiClient;
    private readonly IFaceMatchService faceMatchService;

    public AgentFaceIdfyService(ApplicationDbContext context,
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
        this.customApiClient = customApiCLient;
        this.faceMatchService = faceMatchService;
    }

    [HttpPost]
    public async Task<AppiCheckifyResponse> CaptureAgentId(FaceData data)
    {
        InvestigationTask claim = await caseService.GetCaseById(data.CaseId);
        if (claim?.InvestigationReport == null) return null;

        var agent = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == data.Email);
        var locationRecord = claim.InvestigationReport.ReportTemplate.LocationReport
            .FirstOrDefault(l => l.LocationName == data.LocationName);

        var locationTemplate = await context.LocationReport
            .Include(l => l.AgentIdReport)
            .FirstOrDefaultAsync(l => l.Id == locationRecord.Id);

        var faceReport = locationTemplate.AgentIdReport;

        try
        {
            // 1. Prepare Data & Save Physical File
            var faceBytes = await AgentVerificationHelper.GetBytesFromIFormFile(data.Image);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");

            // 2. Extract Coordinates
            var (lat, lon) = AgentVerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expectedCoords = AgentVerificationHelper.GetExpectedCoordinates(claim);

            // 3. Parallel Service Calls (Orchestration)
            var registeredImage = await File.ReadAllBytesAsync(Path.Combine(webHostEnvironment.ContentRootPath, agent.ProfilePictureUrl));

            var faceTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, Path.GetExtension(fileName));
            var weatherTask = weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = httpClientService.GetRawAddress(lat, lon);
            var mapTask = customApiClient.GetMap(expectedCoords.lat, expectedCoords.lon, double.Parse(lat), double.Parse(lon), "Start", "End", "300", "300", "green", "red");

            await Task.WhenAll(faceTask, weatherTask, addressTask, mapTask);

            // 4. Update Entities
            AgentVerificationHelper.MapMetadataToReports(faceReport, locationTemplate, data, relativePath, fileName, lat, lon);

            var (conf, compImage, sim) = await faceTask;
            var (dist, distM, dur, durS, mapUrl) = await mapTask;

            faceReport.LocationMapUrl = mapUrl;
            faceReport.Duration = dur;
            faceReport.Distance = dist;
            faceReport.DistanceInMetres = distM;
            faceReport.DurationInSeconds = durS;
            faceReport.LocationAddress = await addressTask;
            faceReport.LocationInfo = await weatherTask;
            faceReport.DigitalIdImageMatchConfidence = conf;
            faceReport.Similarity = sim;
            faceReport.ImageValid = sim > 70;

            await File.WriteAllBytesAsync(faceReport.FilePath, compImage);

            await context.SaveChangesAsync();

            return AgentVerificationHelper.CreateResponse(claim, faceReport, compImage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed agent Id match for {Email}", data.Email);
            faceReport.LocationInfo = "No Data";
            faceReport.ValidationExecuted = true;
            await context.SaveChangesAsync();

            byte[] fallback = File.Exists(faceReport.FilePath) ? await File.ReadAllBytesAsync(faceReport.FilePath) : null;
            return AgentVerificationHelper.CreateResponse(claim, faceReport, fallback);
        }
    }
}