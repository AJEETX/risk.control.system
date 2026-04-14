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
    IProcessImageService processImageService,
    ILogger<FaceIdfyService> logger,
    IFileStorageService fileStorageService,
    IPanCardService panCardService,
    IGoogleService googleApi,
    IHttpClientService httpClientService,
    ICustomApiClient customApiCLient) : IDocumentIdfyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly IProcessImageService _processImageService = processImageService;
    private readonly ILogger<FaceIdfyService> _logger = logger;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IPanCardService _panCardService = panCardService;
    private readonly IGoogleService _googleApi = googleApi;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly ICustomApiClient _customApiCLient = customApiCLient;

    public async Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data)
    {
        var claim = await _caseService.GetCaseById(data.CaseId);
        if (claim?.InvestigationReport == null) return null!;
        var location = claim.InvestigationReport.ReportTemplate!.LocationReport.FirstOrDefault(l => l.LocationName == data.LocationName);
        var locationTemplate = await _context.LocationReport.Include(l => l.DocumentIds).FirstOrDefaultAsync(l => l.Id == location!.Id);
        var documentReport = locationTemplate!.DocumentIds!.FirstOrDefault(c => c.ReportName == data.ReportName);
        try
        {
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expected = VerificationHelper.GetExpectedCoordinates(claim);
            var (fileName, relativePath) = await _fileStorageService.SaveAsync(data.Image!, CONSTANTS.CASE, claim.PolicyDetail!.ContractNumber, CONSTANTS.REPORT);
            documentReport!.FilePath = relativePath;
            documentReport.ImageExtension = Path.GetExtension(fileName);
            byte[] docImage = await VerificationHelper.GetBytesFromIFormFile(data.Image!);
            var googleTask = _googleApi.DetectText(documentReport.FilePath);
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
            await ProcessOcrResults(documentReport, docImage, detectedText, claim);
            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.UtcNow;
            locationTemplate.UpdatedBy = data.Email;
            _context.DocumentIdReport.Update(documentReport);
            _context.Investigations.Update(claim);
            await _context.SaveChangesAsync();
            return MapResponse(claim, documentReport, docImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed Document file capture/processing for Case {CaseId}. {AgentEmail}", data.CaseId, data.Email?.Replace("\n", "").Replace("\r", "").Trim());
            return await HandleError(claim, documentReport!);
        }
    }

    private async Task ProcessOcrResults(DocumentIdReport doc, byte[] docImage, IReadOnlyList<TextBlock> ocrResult, InvestigationTask claim)
    {
        if (ocrResult?.Count > 0)
        {
            var company = await _context.ClientCompany.FindAsync(claim.ClientCompanyId);

            if (doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName())
            {
                await _panCardService.Process(docImage, ocrResult, company!, doc, doc.ImageExtension!);
            }
            else
            {
                var compressed = _processImageService.CompressImage(docImage);
                await File.WriteAllBytesAsync(doc.FilePath!, compressed);
                doc.ImageValid = true;
                doc.LocationInfo = ocrResult.FirstOrDefault()?.Text;
            }
        }
        else
        {
            doc.ImageValid = false;
            doc.LocationInfo = "No OCR data detected";
            await File.WriteAllBytesAsync(doc.FilePath!, _processImageService.CompressImage(docImage));
        }
        doc.ValidationExecuted = true;
    }

    private static AppiCheckifyResponse MapResponse(InvestigationTask claim, DocumentIdReport doc, byte[] image)
    {
        return new AppiCheckifyResponse
        {
            BeneficiaryId = claim.BeneficiaryDetail!.BeneficiaryDetailId,
            Image = image,
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