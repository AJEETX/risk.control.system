using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent;

public interface IAgentFaceIdfyService
{
    Task<AppiCheckifyResponse> CaptureAgentId(FaceData data);
}

internal class AgentFaceIdfyService : IAgentFaceIdfyService
{
    private readonly ApplicationDbContext context;
    private readonly IAgentCaseDetailService caseService;
    private readonly IWeatherInfoService weatherInfoService;
    private readonly ILogger<FaceIdfyService> logger;
    private readonly IFileStorageService fileStorageService;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiClient customApiClient;
    private readonly IFaceMatchService faceMatchService;

    public AgentFaceIdfyService(ApplicationDbContext context,
        IAgentCaseDetailService caseService,
        IWeatherInfoService weatherInfoService,
        ILogger<FaceIdfyService> logger,
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

        var locationTemplate = await context.LocationReport.Include(l => l.AgentIdReport).FirstOrDefaultAsync(l => l.Id == locationRecord.Id);

        var agentIdReport = locationTemplate.AgentIdReport;

        try
        {
            // 1. Prepare Data & Save Physical File
            var faceBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");

            // 2. Extract Coordinates
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expectedCoords = VerificationHelper.GetExpectedCoordinates(claim);

            // 3. Parallel Service Calls (Orchestration)
            var registeredImage = await File.ReadAllBytesAsync(Path.Combine(webHostEnvironment.ContentRootPath, agent.ProfilePictureUrl));

            var faceTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, Path.GetExtension(fileName));
            var weatherTask = weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = httpClientService.GetRawAddress(lat, lon);
            var mapTask = customApiClient.GetMap(expectedCoords.lat, expectedCoords.lon, double.Parse(lat), double.Parse(lon), "Start", "End", "300", "300", "green", "red");

            await Task.WhenAll(faceTask, weatherTask, addressTask, mapTask);

            // 4. Update Entities
            AgentFaceIdfyHelper.MapMetadataToReport(agentIdReport, locationTemplate, data, relativePath, fileName, lat, lon);

            var (conf, compImage, sim) = await faceTask;
            var (dist, distM, dur, durS, mapUrl) = await mapTask;

            agentIdReport.LocationMapUrl = mapUrl;
            agentIdReport.Duration = dur;
            agentIdReport.Distance = dist;
            agentIdReport.DistanceInMetres = distM;
            agentIdReport.DurationInSeconds = durS;
            agentIdReport.LocationAddress = await addressTask;
            agentIdReport.LocationInfo = await weatherTask;
            agentIdReport.DigitalIdImageMatchConfidence = conf;
            agentIdReport.Similarity = sim;
            agentIdReport.ImageValid = sim > 70;

            await File.WriteAllBytesAsync(agentIdReport.FilePath, compImage);

            await context.SaveChangesAsync();

            return AgentFaceIdfyHelper.CreateResponse(claim, agentIdReport, compImage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed Agent face Id match for CaseId {Id}. {AgentEmail}", data.CaseId, data.Email);
            return await HandleError(claim, agentIdReport);
        }
    }

    private async Task<AppiCheckifyResponse> HandleError(InvestigationTask claim, AgentIdReport agentIdReport)
    {
        agentIdReport.LocationInfo = "No Data";
        agentIdReport.ValidationExecuted = true;
        agentIdReport.ImageValid = false;
        await context.SaveChangesAsync();
        byte[] fallback = File.Exists(agentIdReport.FilePath) ? await File.ReadAllBytesAsync(agentIdReport.FilePath) : null;
        return AgentFaceIdfyHelper.CreateResponse(claim, agentIdReport, fallback);
    }
}