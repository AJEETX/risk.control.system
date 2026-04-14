using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent;

public interface IAgentFaceIdfyService
{
    Task<AppiCheckifyResponse> CaptureAgentId(FaceData data);
}

internal class AgentFaceIdfyService(ApplicationDbContext context,
    IAgentCaseDetailService caseService,
    IWeatherInfoService weatherInfoService,
    ILogger<FaceIdfyService> logger,
    IFileStorageService fileStorageService,
    IWebHostEnvironment env,
    IHttpClientService httpClientService,
    ICustomApiClient customApiCLient,
    IFaceMatchService faceMatchService) : IAgentFaceIdfyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly IWeatherInfoService _weatherInfoService = weatherInfoService;
    private readonly ILogger<FaceIdfyService> _logger = logger;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IWebHostEnvironment _env = env;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly ICustomApiClient _customApiClient = customApiCLient;
    private readonly IFaceMatchService _faceMatchService = faceMatchService;

    [HttpPost]
    public async Task<AppiCheckifyResponse> CaptureAgentId(FaceData data)
    {
        InvestigationTask claim = await _caseService.GetCaseById(data.CaseId);
        if (claim?.InvestigationReport == null) return null!;

        var agent = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == data.Email);
        var locationRecord = claim.InvestigationReport.ReportTemplate!.LocationReport
            .FirstOrDefault(l => l.LocationName == data.LocationName);

        var locationTemplate = await _context.LocationReport.Include(l => l.AgentIdReport).FirstOrDefaultAsync(l => l.Id == locationRecord!.Id);

        var agentIdReport = locationTemplate!.AgentIdReport;

        try
        {
            // 1. Prepare Data & Save Physical File
            var faceBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image!);
            var (faceImageFileName, relativePath) = await _fileStorageService.SaveAsync(data.Image!, CONSTANTS.CASE, claim.PolicyDetail!.ContractNumber, CONSTANTS.REPORT);

            // 2. Extract Coordinates
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong!);
            var expectedCoords = VerificationHelper.GetExpectedCoordinates(claim);

            // 3. Parallel Service Calls (Orchestration)
            var registeredImage = await File.ReadAllBytesAsync(Path.Combine(_env.ContentRootPath, agent!.ProfilePictureUrl!));

            var faceTask = _faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, Path.GetExtension(faceImageFileName));
            var weatherTask = _weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = _httpClientService.GetRawAddress(lat, lon);
            var mapTask = _customApiClient.GetMap(expectedCoords.lat, expectedCoords.lon, double.Parse(lat), double.Parse(lon));

            await Task.WhenAll(faceTask, weatherTask, addressTask, mapTask);

            // 4. Update Entities
            AgentFaceIdfyHelper.MapMetadataToReport(agentIdReport!, locationTemplate, data, relativePath, faceImageFileName, lat, lon);

            var (conf, compImage, sim) = await faceTask;
            var (dist, distM, dur, durS, mapUrl) = await mapTask;

            agentIdReport!.LocationMapUrl = mapUrl;
            agentIdReport.Duration = dur;
            agentIdReport.Distance = dist;
            agentIdReport.DistanceInMetres = distM;
            agentIdReport.DurationInSeconds = durS;
            agentIdReport.LocationAddress = await addressTask;
            agentIdReport.LocationInfo = await weatherTask;
            agentIdReport.DigitalIdImageMatchConfidence = conf;
            agentIdReport.Similarity = sim;
            agentIdReport.ImageValid = sim > 70;

            await File.WriteAllBytesAsync(agentIdReport.FilePath!, compImage);

            await _context.SaveChangesAsync();

            return AgentFaceIdfyHelper.CreateResponse(claim, agentIdReport, compImage);
        }
        catch (Exception ex)
        {
            var sanitizedEmail = data.Email?.Replace("\n", "").Replace("\r", "").Trim();
            _logger.LogError(ex, "Failed Agent face Id match for CaseId {Id}. {AgentEmail}", data.CaseId, sanitizedEmail);
            return await HandleError(claim, agentIdReport!);
        }
    }

    private async Task<AppiCheckifyResponse> HandleError(InvestigationTask claim, AgentIdReport agentIdReport)
    {
        agentIdReport.LocationInfo = "No Data";
        agentIdReport.ValidationExecuted = true;
        agentIdReport.ImageValid = false;
        await _context.SaveChangesAsync();
        byte[] fallback = File.Exists(agentIdReport.FilePath) ? await File.ReadAllBytesAsync(agentIdReport.FilePath) : null!;
        return AgentFaceIdfyHelper.CreateResponse(claim, agentIdReport, fallback);
    }
}