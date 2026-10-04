using Hangfire;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Services.Agent;

public interface IMediaIdfyService
{
    Task<AppiCheckifyResponse> CaptureMedia(DocumentData data);
}

internal class MediaIdfyService(ApplicationDbContext context,
    IAgentCaseDetailService caseService,
    IWeatherInfoService weatherInfoService,
    ILogger<FaceIdfyService> logger,
    IFileStorageService fileStorageService,
    IBackgroundJobClient backgroundJobClient,
    IHttpClientService httpClientService,
    ICustomApiClient customApiClient) : IMediaIdfyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly IWeatherInfoService _weatherInfoService = weatherInfoService;
    private readonly ILogger<FaceIdfyService> _logger = logger;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly ICustomApiClient _customApiClient = customApiClient;

    public async Task<AppiCheckifyResponse> CaptureMedia(DocumentData data)
    {
        var caseDetail = await _caseService.GetCaseByIdForMedia(data.CaseId);
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

        var locationTemplate = await _context.LocationReport.Include(l => l.MediaReports).FirstOrDefaultAsync(l => l.Id == locationRecord!.Id);
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

        var mediaReport = locationTemplate!.MediaReports!.FirstOrDefault(c => c.ReportName == data.ReportName);
        if (mediaReport == null)
        {
            _logger.LogError("Media report not found for Case {CaseId}, LocationName {LocationName}, and ReportName {ReportName}", data.CaseId, data.LocationName, data.ReportName);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                Valid = false,
                LocationLongLat = "No Data",
                LocationTime = DateTime.UtcNow
            };
        }

        if (!string.IsNullOrWhiteSpace(mediaReport!.FilePath))
        {
            _fileStorageService.DeleteFile(mediaReport.FilePath);
        }
        if (!string.IsNullOrWhiteSpace(mediaReport!.OriginalFilePath))
        {
            _fileStorageService.DeleteFile(mediaReport.OriginalFilePath);
        }
        try
        {
            // 1. Prepare Data & Coordinates
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expected = VerificationHelper.GetExpectedCoordinates(caseDetail);
            byte[] fileBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image!);

            // 2. Storage & Metadata
            var (fileName, relativePath) = await _fileStorageService.SaveMediaAsync(data.Image!, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT);
            MediaIdfyHelper.UpdateMediaMetadata(mediaReport!, relativePath, fileName, lat, lon);
            MediaIdfyHelper.DetermineMediaType(mediaReport!, data.Image!.ContentType);
            _backgroundJobClient.Enqueue<ISpeech2TextService>(service => service.ConvertMediaSpeech(relativePath, mediaReport!.Id));

            // 3. Parallel Service Orchestration
            var weatherTask = _weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = _httpClientService.GetRawAddress(lat, lon);
            var mapTask = _customApiClient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon));

            await Task.WhenAll(weatherTask, addressTask, mapTask);

            // 4. Update Results
            var (dist, distM, dur, durS, mapUrl) = await mapTask;
            mediaReport!.LocationMapUrl = mapUrl;
            mediaReport.Duration = dur;
            mediaReport.Distance = dist;
            mediaReport.DistanceInMetres = distM;
            mediaReport.DurationInSeconds = durS;
            mediaReport.LocationAddress = await addressTask;
            mediaReport.LocationInfo = await weatherTask;
            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.UtcNow;
            locationTemplate.UpdatedBy = data.Email;
            await _context.SaveChangesAsync();

            return new AppiCheckifyResponse { ByteImage = fileBytes };
        }
        catch (Exception ex)
        {
            var sanitizedEmail = data.Email?.Replace("\n", "").Replace("\r", "").Trim();
            _logger.LogError(ex, "Failed media file capture for Case {CaseId}. {AgentEmail}", data.CaseId, sanitizedEmail);
            return await HandleMediaError(caseDetail, mediaReport!);
        }
    }

    private async Task<AppiCheckifyResponse> HandleMediaError(InvestigationTask claim, MediaReport media)
    {
        media.LocationInfo = "No Data";
        media.ImageValid = false;
        media.LocationAddress = "No Address data";
        media.ValidationExecuted = true;

        await _context.SaveChangesAsync();

        return new AppiCheckifyResponse
        {
            BeneficiaryId = claim?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
            LocationLongLat = media?.LongLat,
            LocationTime = media?.LongLatTime,
            Valid = false
        };
    }
}