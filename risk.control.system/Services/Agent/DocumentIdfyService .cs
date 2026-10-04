using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Services.Agent;

public interface IDocumentIdfyService
{
    Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data);
}

internal class DocumentIdfyService(ApplicationDbContext context,
    IAgentCaseDetailService caseService,
    IProcessDocumentService processDocumentService,
    ILogger<DocumentIdfyService> logger,
    IFileStorageService fileStorageService,
    IGoogleOcrService googleApi,
    IHttpClientService httpClientService,
    ICustomApiClient customApiCLient) : IDocumentIdfyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly IProcessDocumentService _processDocumentService = processDocumentService;
    private readonly ILogger<DocumentIdfyService> _logger = logger;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IGoogleOcrService _googleApi = googleApi;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly ICustomApiClient _customApiCLient = customApiCLient;

    public async Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data)
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

        var locationTemplate = await _context.LocationReport.Include(l => l.DocumentIds).FirstOrDefaultAsync(l => l.Id == locationRecord!.Id);
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

        var documentReport = locationTemplate!.DocumentIds!.FirstOrDefault(c => c.ReportName == data.ReportName);
        if (documentReport == null)
        {
            _logger.LogError("Document report not found for Case {CaseId}, LocationName {LocationName}, and ReportName {ReportName}", data.CaseId, data.LocationName, data.ReportName);
            return new AppiCheckifyResponse
            {
                BeneficiaryId = caseDetail?.BeneficiaryDetail?.BeneficiaryDetailId ?? 0,
                Valid = false,
                LocationLongLat = "No Data",
                LocationTime = DateTime.UtcNow
            };
        }

        if (!string.IsNullOrWhiteSpace(documentReport!.FilePath))
        {
            _fileStorageService.DeleteFile(documentReport.FilePath);
        }
        if (!string.IsNullOrWhiteSpace(documentReport!.OriginalFilePath))
        {
            _fileStorageService.DeleteFile(documentReport.OriginalFilePath);
        }
        try
        {
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expected = VerificationHelper.GetExpectedCoordinates(caseDetail);
            var docName = documentReport!.ReportName;
            var extension = Path.GetExtension(data.Image!.FileName.ToLowerInvariant());
            var (fileName, relativePath) = await _fileStorageService.SaveAsync(data.Image!, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.TEMP_REPORT, null, $"{docName}{extension}");
            documentReport!.FilePath = relativePath;

            documentReport.ImageExtension = Path.GetExtension(fileName);
            var googleTask = _googleApi.DetectText(documentReport.FilePath!);
            //var googleTask = googleApi.DetectTextAsync(documentReport.FilePath);
            var addressTask = _httpClientService.GetRawAddress(lat, lon);
            var mapTask = _customApiCLient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon));
            await Task.WhenAll(googleTask, addressTask, mapTask /*, ocrTask*/);
            var (dist, distM, dur, durS, mapUrl) = await mapTask;
            documentReport.LocationMapUrl = mapUrl;
            documentReport.Distance = dist;
            documentReport.DistanceInMetres = distM;
            documentReport.DurationInSeconds = durS;
            documentReport.Duration = dur;
            documentReport.LocationAddress = await addressTask;
            documentReport.LongLat = $"Latitude = {lat}, Longitude = {lon}";
            documentReport.LongLatTime = DateTime.UtcNow;
            var detectedText = await googleTask;

            byte[] docImageBytes = await VerificationHelper.GetBytesFromIFormFile(data.Image!);

            await _processDocumentService.ProcessOcrResults(documentReport, docImageBytes, detectedText, caseDetail, extension);

            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.UtcNow;
            locationTemplate.UpdatedBy = data.Email;
            _context.DocumentIdReport.Update(documentReport);
            _context.Investigations.Update(caseDetail);
            await _context.SaveChangesAsync();
            return MapResponse(caseDetail, documentReport, docImageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed Document file capture for Case {CaseId}. {AgentEmail}", data.CaseId, data.Email?.Replace("\n", "").Replace("\r", "").Trim());
            return await HandleError(caseDetail, documentReport!);
        }
    }

    private static AppiCheckifyResponse MapResponse(InvestigationTask claim, DocumentIdReport doc, byte[] image)
    {
        return new AppiCheckifyResponse
        {
            BeneficiaryId = claim.BeneficiaryDetail!.BeneficiaryDetailId,
            ByteImage = image,
            OcrImage = doc.FilePath,
            OcrLongLat = doc.LongLat,
            OcrTime = doc.LongLatTime,
            Valid = doc.ImageValid
        };
    }

    private async Task<AppiCheckifyResponse> HandleError(InvestigationTask claim, DocumentIdReport doc)
    {
        doc.LocationInfo = "No Data";
        doc.ImageValid = false;
        doc.ValidationExecuted = true;
        await _context.SaveChangesAsync();

        var img = File.Exists(doc.FilePath) ? await File.ReadAllBytesAsync(doc.FilePath) : null;
        return MapResponse(claim, doc, img!);
    }
}