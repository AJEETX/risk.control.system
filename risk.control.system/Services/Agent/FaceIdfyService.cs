using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent;

public interface IFaceIdfyService
{
    Task<AppiCheckifyResponse> CaptureFaceId(FaceData data);
}

internal class FaceIdfyService : IFaceIdfyService
{
    private readonly ApplicationDbContext context;
    private readonly IAgentCaseDetailService caseService;
    private readonly IWeatherInfoService weatherInfoService;
    private readonly ILogger<FaceIdfyService> logger;
    private readonly IFileStorageService fileStorageService;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiClient customApiCLient;
    private readonly IFaceMatchService faceMatchService;

    public FaceIdfyService(ApplicationDbContext context,
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
        this.customApiCLient = customApiCLient;
        this.faceMatchService = faceMatchService;
    }

    public async Task<AppiCheckifyResponse> CaptureFaceId(FaceData data)
    {
        var caseDetail = await caseService.GetCaseById(data.CaseId);
        if (caseDetail?.InvestigationReport == null) return null!;
        var agent = await context.ApplicationUser.FirstAsync(u => u.Email == data.Email);
        var location = caseDetail.InvestigationReport.ReportTemplate!.LocationReport.First(l => l.LocationName == data.LocationName);
        var locationTemplate = await context.LocationReport.Include(l => l.FaceIds).FirstAsync(l => l.Id == location.Id);
        var faceIdReport = locationTemplate.FaceIds!.First(c => c.ReportName == data.ReportName);

        try
        {
            bool isCustomer = faceIdReport.ReportName == DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName();
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong!);
            var expected = VerificationHelper.GetExpectedCoordinates(caseDetail);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image!, "Case", caseDetail.PolicyDetail!.ContractNumber, "report");
            var faceBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image!);
            var regPath = Path.Combine(webHostEnvironment.ContentRootPath, FaceIdfyHelper.GetRegisteredImagePath(caseDetail, isCustomer));
            var registeredImage = await File.ReadAllBytesAsync(regPath);
            var faceTask = faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, Path.GetExtension(fileName));
            var weatherTask = weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = httpClientService.GetRawAddress(lat, lon);
            var mapTask = customApiCLient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon));
            await Task.WhenAll(faceTask, weatherTask, addressTask, mapTask);
            FaceIdfyHelper.MapMetadataToReport(faceIdReport, locationTemplate, data, relativePath, Path.GetExtension(fileName), lat, lon);
            var (conf, compImg, sim) = await faceTask;
            var (dist, distM, dur, durS, mapUrl) = await mapTask;
            faceIdReport.LocationMapUrl = mapUrl;
            faceIdReport.Distance = dist;
            faceIdReport.Duration = dur;
            faceIdReport.LocationInfo = await weatherTask;
            faceIdReport.LocationAddress = await addressTask;
            faceIdReport.MatchConfidence = conf;
            faceIdReport.Similarity = sim;
            faceIdReport.ImageValid = sim > 70;
            await File.WriteAllBytesAsync(faceIdReport.FilePath!, compImg);
            await context.SaveChangesAsync();
            return FaceIdfyHelper.BuildResponse(caseDetail, faceIdReport, compImg);
        }
        catch (Exception ex)
        {
            var sanitizedEmail = data.Email?.Replace("\n", "").Replace("\r", "").Trim();
            logger.LogError(ex, "Failed Face Id processing for {CaseId}. {AgentEmail}", data.CaseId, sanitizedEmail);
            return await HandleError(caseDetail, faceIdReport);
        }
    }

    private async Task<AppiCheckifyResponse> HandleError(InvestigationTask claim, FaceIdReport faceIdReport)
    {
        faceIdReport.LocationInfo = "No Data";
        faceIdReport.ValidationExecuted = true;
        faceIdReport.ImageValid = false;
        await context.SaveChangesAsync();
        var img = File.Exists(faceIdReport.FilePath) ? await File.ReadAllBytesAsync(faceIdReport.FilePath) : null;
        return FaceIdfyHelper.BuildResponse(claim, faceIdReport, img!);
    }
}