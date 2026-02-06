using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent;

public interface IMediaIdfyService
{
    Task<AppiCheckifyResponse> CaptureMedia(DocumentData data);
}

internal class MediaIdfyService : IMediaIdfyService
{
    private readonly ApplicationDbContext context;
    private readonly IAgentCaseDetailService caseService;
    private readonly IWeatherInfoService weatherInfoService;
    private readonly ILogger<FaceIdfyService> logger;
    private readonly IFileStorageService fileStorageService;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiClient customApiCLient;

    public MediaIdfyService(ApplicationDbContext context,
        IAgentCaseDetailService caseService,
        IWeatherInfoService weatherInfoService,
        ILogger<FaceIdfyService> logger,
        IFileStorageService fileStorageService,
        IHttpClientService httpClientService,
        ICustomApiClient customApiCLient)
    {
        this.context = context;
        this.caseService = caseService;
        this.weatherInfoService = weatherInfoService;
        this.logger = logger;
        this.fileStorageService = fileStorageService;
        this.httpClientService = httpClientService;
        this.customApiCLient = customApiCLient;
    }

    public async Task<AppiCheckifyResponse> CaptureMedia(DocumentData data)
    {
        InvestigationTask claim = await caseService.GetCaseByIdForMedia(data.CaseId);
        if (claim?.InvestigationReport == null) return null;

        var location = claim.InvestigationReport.ReportTemplate.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);

        var locationTemplate = await context.LocationReport.Include(l => l.MediaReports).FirstOrDefaultAsync(l => l.Id == location.Id);

        var media = locationTemplate.MediaReports.FirstOrDefault(c => c.ReportName == data.ReportName);

        try
        {
            // 1. Prepare Data & Coordinates
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expected = VerificationHelper.GetExpectedCoordinates(claim);
            byte[] fileBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image);

            // 2. Storage & Metadata
            var (fileName, relativePath) = await fileStorageService.SaveMediaAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");
            MediaIdfyHelper.UpdateMediaMetadata(media, relativePath, fileName, lat, lon);
            MediaIdfyHelper.DetermineMediaType(media, data.Image.ContentType);

            // 3. Parallel Service Orchestration
            var weatherTask = weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = httpClientService.GetRawAddress(lat, lon);
            var mapTask = customApiCLient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon), "Start", "End", "300", "300", "green", "red");

            await Task.WhenAll(weatherTask, addressTask, mapTask);

            // 4. Update Results
            var (dist, distM, dur, durS, mapUrl) = await mapTask;
            media.LocationMapUrl = mapUrl;
            media.Duration = dur;
            media.Distance = dist;
            media.DistanceInMetres = distM;
            media.DurationInSeconds = durS;
            media.LocationAddress = await addressTask;
            media.LocationInfo = await weatherTask;

            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.UtcNow;
            locationTemplate.UpdatedBy = data.Email;
            await context.SaveChangesAsync();

            return new AppiCheckifyResponse { Image = fileBytes };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed media file capture for Case {CaseId}. {AgentEmail}", data.CaseId, data.Email);
            return await HandleMediaError(claim, media);
        }
    }

    private async Task<AppiCheckifyResponse> HandleMediaError(InvestigationTask claim, MediaReport media)
    {
        media.LocationInfo = "No Data";
        media.ImageValid = false;
        media.LocationAddress = "No Address data";
        media.ValidationExecuted = true;

        await context.SaveChangesAsync();

        return new AppiCheckifyResponse
        {
            BeneficiaryId = claim?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
            LocationLongLat = media?.LongLat,
            LocationTime = media?.LongLatTime,
            Valid = false
        };
    }
}