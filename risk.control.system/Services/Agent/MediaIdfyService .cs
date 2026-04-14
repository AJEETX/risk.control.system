using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

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
    IHttpClientService httpClientService,
    ICustomApiClient customApiClient) : IMediaIdfyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly IWeatherInfoService _weatherInfoService = weatherInfoService;
    private readonly ILogger<FaceIdfyService> _logger = logger;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly ICustomApiClient _customApiClient = customApiClient;

    public async Task<AppiCheckifyResponse> CaptureMedia(DocumentData data)
    {
        InvestigationTask claim = await _caseService.GetCaseByIdForMedia(data.CaseId);
        if (claim?.InvestigationReport == null) return null!;

        var location = claim.InvestigationReport.ReportTemplate!.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

        var locationTemplate = await _context.LocationReport.Include(l => l.MediaReports).FirstOrDefaultAsync(l => l.Id == location!.Id);

        var media = locationTemplate!.MediaReports!.FirstOrDefault(c => c.ReportName == data.ReportName);

        try
        {
            // 1. Prepare Data & Coordinates
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expected = VerificationHelper.GetExpectedCoordinates(claim);
            byte[] fileBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image!);

            // 2. Storage & Metadata
            var (fileName, relativePath) = await _fileStorageService.SaveMediaAsync(data.Image!, CONSTANTS.CASE, claim.PolicyDetail!.ContractNumber, CONSTANTS.REPORT);
            MediaIdfyHelper.UpdateMediaMetadata(media!, relativePath, fileName, lat, lon);
            MediaIdfyHelper.DetermineMediaType(media!, data.Image!.ContentType);

            // 3. Parallel Service Orchestration
            var weatherTask = _weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = _httpClientService.GetRawAddress(lat, lon);
            var mapTask = _customApiClient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon));

            await Task.WhenAll(weatherTask, addressTask, mapTask);

            // 4. Update Results
            var (dist, distM, dur, durS, mapUrl) = await mapTask;
            media!.LocationMapUrl = mapUrl;
            media.Duration = dur;
            media.Distance = dist;
            media.DistanceInMetres = distM;
            media.DurationInSeconds = durS;
            media.LocationAddress = await addressTask;
            media.LocationInfo = await weatherTask;

            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.UtcNow;
            locationTemplate.UpdatedBy = data.Email;
            await _context.SaveChangesAsync();

            return new AppiCheckifyResponse { Image = fileBytes };
        }
        catch (Exception ex)
        {
            var sanitizedEmail = data.Email?.Replace("\n", "").Replace("\r", "").Trim();
            _logger.LogError(ex, "Failed media file capture for Case {CaseId}. {AgentEmail}", data.CaseId, sanitizedEmail);
            return await HandleMediaError(claim, media!);
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