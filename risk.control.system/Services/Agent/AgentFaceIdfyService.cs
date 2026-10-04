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

    public async Task<AppiCheckifyResponse> CaptureAgentId(FaceData data)
    {
        var caseDetail = await _caseService.GetCaseById(data.CaseId);
        if (caseDetail?.InvestigationReport == null)
        {
            _logger.LogError("Case not found for CaseId {CaseId}", data.CaseId);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                Valid = false,
                LocationLongLat = "No Data",
                LocationTime = DateTime.UtcNow
            };
        }

        var agent = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == data.Email);
        var locationRecord = caseDetail.InvestigationReport.ReportTemplate!.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);
        if (locationRecord == null)
        {
            _logger.LogError("Location not found for Case {CaseId} and LocationName {LocationName}", data.CaseId, data.LocationName);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                Valid = false,
                LocationLongLat = "No Data",
                LocationTime = DateTime.UtcNow
            };
        }

        var locationTemplate = await _context.LocationReport.Include(l => l.AgentIdReport).FirstOrDefaultAsync(l => l.Id == locationRecord!.Id);
        if (locationTemplate == null)
        {
            _logger.LogError("Location template not found for Case {CaseId} and LocationName {LocationName}", data.CaseId, data.LocationName);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                Valid = false,
                LocationLongLat = "No Data",
                LocationTime = DateTime.UtcNow
            };
        }

        var agentIdReport = locationTemplate!.AgentIdReport;
        if (!string.IsNullOrWhiteSpace(agentIdReport!.FilePath))
        {
            _fileStorageService.DeleteFile(agentIdReport.FilePath);
        }
        if (!string.IsNullOrWhiteSpace(agentIdReport!.OriginalFilePath))
        {
            _fileStorageService.DeleteFile(agentIdReport.OriginalFilePath);
        }
        try
        {
            // 1. Prepare Data & Save Physical File
            var faceBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image!);
            var imageExtension = Path.GetExtension(data.Image!.FileName.ToLowerInvariant());
            var (faceImageFileName, relativePath) = await _fileStorageService.SaveAsync(data.Image!, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.TEMP_REPORT, null, $"agent{imageExtension}");
            var (foriginalFaceImageFileName, originalRelativePath) = await _fileStorageService.SaveAsync(faceBytes, imageExtension, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT, null, $"agent{imageExtension}");

            // 2. Extract Coordinates
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong!);
            var expectedCoords = VerificationHelper.GetExpectedCoordinates(caseDetail);

            // 3. Parallel Service Calls (Orchestration)
            var registeredImage = await File.ReadAllBytesAsync(Path.Combine(_env.ContentRootPath, agent!.ProfilePictureUrl!));

            var faceTask = _faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, Path.GetExtension(faceImageFileName));
            var faceDetailTask = _faceMatchService.GetPersonDetailFromFace(faceBytes);
            var weatherTask = _weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = _httpClientService.GetRawAddress(lat, lon);
            var mapTask = _customApiClient.GetMap(expectedCoords.lat, expectedCoords.lon, double.Parse(lat), double.Parse(lon));

            await Task.WhenAll(faceTask, weatherTask, addressTask, mapTask, faceDetailTask);

            // 4. Update Entities
            AgentFaceIdfyHelper.MapMetadataToReport(agentIdReport!, locationTemplate, data, relativePath, originalRelativePath, faceImageFileName, lat, lon);

            var (conf, agentImage, sim) = await faceTask;
            var (dist, distM, dur, durS, mapUrl) = await mapTask;

            agentIdReport!.LocationMapUrl = mapUrl;
            agentIdReport.Duration = dur;
            agentIdReport.Distance = dist;
            agentIdReport.DistanceInMetres = distM;
            agentIdReport.DurationInSeconds = durS;
            agentIdReport.LocationAddress = await addressTask;
            agentIdReport.FaceResult = await faceDetailTask;
            agentIdReport.LocationInfo = await weatherTask;
            agentIdReport.DigitalIdImageMatchConfidence = conf;
            agentIdReport.Similarity = sim;
            agentIdReport.ImageValid = sim > 70;

            await File.WriteAllBytesAsync(agentIdReport.FilePath!, agentImage);

            await _context.SaveChangesAsync();

            return AgentFaceIdfyHelper.CreateResponse(caseDetail, agentIdReport, agentImage);
        }
        catch (Exception ex)
        {
            var sanitizedEmail = data.Email?.Replace("\n", "").Replace("\r", "").Trim();
            _logger.LogError(ex, "Failed Agent face Id match for CaseId {Id}. {AgentEmail}", data.CaseId, sanitizedEmail);
            return await HandleError(caseDetail, agentIdReport!);
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