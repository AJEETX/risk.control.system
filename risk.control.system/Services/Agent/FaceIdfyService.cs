using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent;

public interface IFaceIdfyService
{
    Task<AppiCheckifyResponse> CaptureFaceId(FaceData data);
}

internal class FaceIdfyService(ApplicationDbContext context,
    IAgentCaseDetailService caseService,
    IWeatherInfoService weatherInfoService,
    ILogger<FaceIdfyService> logger,
    IFileStorageService fileStorageService,
    IWebHostEnvironment env,
    IHttpClientService httpClientService,
    ICustomApiClient customApiClient,
    IFaceMatchService faceMatchService) : IFaceIdfyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly IWeatherInfoService _weatherInfoService = weatherInfoService;
    private readonly ILogger<FaceIdfyService> _logger = logger;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IWebHostEnvironment _env = env;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly ICustomApiClient _customApiClient = customApiClient;
    private readonly IFaceMatchService _faceMatchService = faceMatchService;

    public async Task<AppiCheckifyResponse> CaptureFaceId(FaceData data)
    {
        var caseDetail = await _caseService.GetCaseById(data.CaseId);
        if (caseDetail?.InvestigationReport == null) return null!;
        var agent = await _context.ApplicationUser.FirstAsync(u => u.Email == data.Email);
        var location = caseDetail.InvestigationReport.ReportTemplate!.LocationReport.First(l => l.LocationName == data.LocationName);
        var locationTemplate = await _context.LocationReport.Include(l => l.FaceIds).FirstAsync(l => l.Id == location.Id);
        var faceIdReport = locationTemplate.FaceIds!.First(c => c.ReportName == data.ReportName);

        try
        {
            bool isCustomer = faceIdReport.ReportName == DigitalIdReportType.CUSTOMER_FACE.GetEnumDisplayName();
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong!);
            var expected = VerificationHelper.GetExpectedCoordinates(caseDetail);
            var (fileName, relativePath) = await _fileStorageService.SaveAsync(data.Image!, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT);
            var faceBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image!);
            var regPath = Path.Combine(_env.ContentRootPath, FaceIdfyHelper.GetRegisteredImagePath(caseDetail, isCustomer));
            var registeredImage = await File.ReadAllBytesAsync(regPath);
            var faceTask = _faceMatchService.GetFaceMatchAsync(registeredImage, faceBytes, Path.GetExtension(fileName));
            var weatherTask = _weatherInfoService.GetWeatherAsync(lat, lon);
            var addressTask = _httpClientService.GetRawAddress(lat, lon);
            var mapTask = _customApiClient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon));
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
            await _context.SaveChangesAsync();
            return FaceIdfyHelper.BuildResponse(caseDetail, faceIdReport, compImg);
        }
        catch (Exception ex)
        {
            var sanitizedEmail = data.Email?.Replace("\n", "").Replace("\r", "").Trim();
            _logger.LogError(ex, "Failed Face Id processing for {CaseId}. {AgentEmail}", data.CaseId, sanitizedEmail);
            return await HandleError(caseDetail, faceIdReport);
        }
    }

    private async Task<AppiCheckifyResponse> HandleError(InvestigationTask claim, FaceIdReport faceIdReport)
    {
        faceIdReport.LocationInfo = "No Data";
        faceIdReport.ValidationExecuted = true;
        faceIdReport.ImageValid = false;
        await _context.SaveChangesAsync();
        var img = File.Exists(faceIdReport.FilePath) ? await File.ReadAllBytesAsync(faceIdReport.FilePath) : null;
        return FaceIdfyHelper.BuildResponse(claim, faceIdReport, img!);
    }
}