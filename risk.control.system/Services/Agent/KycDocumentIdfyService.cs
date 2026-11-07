using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;
using risk.control.system.Services.Tool;

namespace risk.control.system.Services.Agent;

public interface IKycDocumentIdfyService
{
    Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data, InvestigationTask caseDetail, DocumentIdReport documentReport, LocationReport locationReport);
}

internal class KycDocumentIdfyService(ApplicationDbContext context,
    IAgentCaseDetailService caseService,
    IProcessKycDocumentService processKycDocumentService,
    ILogger<KycDocumentIdfyService> logger,
    IFileStorageService fileStorageService,
    IGoogleOcrService googleApi,
    IHttpClientService httpClientService,
    ICustomApiClient customApiCLient) : IKycDocumentIdfyService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAgentCaseDetailService _caseService = caseService;
    private readonly IProcessKycDocumentService _processKycDocumentService = processKycDocumentService;
    private readonly ILogger<KycDocumentIdfyService> _logger = logger;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IGoogleOcrService _googleApi = googleApi;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly ICustomApiClient _customApiCLient = customApiCLient;

    public async Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data, InvestigationTask caseDetail, DocumentIdReport documentReport, LocationReport locationReport)
    {
        var documentFileForPage = documentReport!.DocumentFiles.FirstOrDefault(d => d.PageNumber == data.PageNumber);

        if (documentFileForPage != null && !string.IsNullOrWhiteSpace(documentFileForPage.FilePath))
        {
            _fileStorageService.DeleteFile(documentFileForPage.FilePath);
        }
        if (documentFileForPage != null && !string.IsNullOrWhiteSpace(documentFileForPage.OriginalFilePath))
        {
            _fileStorageService.DeleteFile(documentFileForPage.OriginalFilePath);
        }
        try
        {
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expected = VerificationHelper.GetExpectedCoordinates(caseDetail);
            var docName = documentReport!.ReportName;
            var extension = Path.GetExtension(data.Image!.FileName.ToLowerInvariant());
            var isClaim = caseDetail.PolicyDetail!.InsuranceType == InsuranceType.CLAIM;
            docName = isClaim ? $"Beneficiary-{docName}" : $"Life-Assured-{docName}";
            var (fileName, relativePath) = await _fileStorageService.SaveAsync(data.Image!, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.TEMP_REPORT, null, $"{docName}_{data.PageNumber}{extension}");
            if (documentFileForPage != null)
            {
                documentFileForPage!.FilePath = relativePath;
            }
            else
                documentFileForPage = new DocumentFile
                {
                    PageNumber = data.PageNumber,
                    FilePath = relativePath,
                    OriginalFilePath = relativePath
                };
            documentReport.DocumentFiles.Add(documentFileForPage);

            var googleTask = _googleApi.DetectText(documentFileForPage.FilePath!);
            var addressTask = _httpClientService.GetRawAddress(lat, lon);
            var mapTask = _customApiCLient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon));
            await Task.WhenAll(googleTask, addressTask, mapTask);
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

            await _processKycDocumentService.ProcessOcrResults(documentReport, docImageBytes, detectedText, caseDetail, extension, docName, data.PageNumber);

            locationReport.ValidationExecuted = true;
            locationReport.Updated = DateTime.UtcNow;
            locationReport.UpdatedBy = data.Email;
            _context.DocumentIdReport.Update(documentReport);
            _context.Investigations.Update(caseDetail);
            await _context.SaveChangesAsync();
            return MapResponse(caseDetail, documentReport, docImageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed Document file capture for Case {CaseId}, Agent Email {AgentEmail}", data.CaseId, data.Email?.Replace("\n", "").Replace("\r", "").Trim());
            return await HandleError(caseDetail, documentReport!, data.PageNumber);
        }
    }

    private static AppiCheckifyResponse MapResponse(InvestigationTask claim, DocumentIdReport doc, byte[] image, int PageNumber = 1)
    {
        return new AppiCheckifyResponse
        {
            BeneficiaryId = claim.BeneficiaryDetail!.BeneficiaryDetailId,
            ByteImage = image,
            OcrImage = doc.DocumentFiles.FirstOrDefault(d => d.PageNumber == PageNumber)?.FilePath,
            OcrLongLat = doc.LongLat,
            OcrTime = doc.LongLatTime,
            Valid = doc.ImageValid
        };
    }

    private async Task<AppiCheckifyResponse> HandleError(InvestigationTask claim, DocumentIdReport doc, int PageNumber = 1)
    {
        doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == PageNumber)!.OcrText = "No Data";
        doc.ImageValid = false;
        doc.ValidationExecuted = true;
        await _context.SaveChangesAsync();
        var filePath = doc.DocumentFiles.FirstOrDefault(d => d.PageNumber == PageNumber)?.FilePath;
        var img = File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath) : null;
        return MapResponse(claim, doc, img!, PageNumber);
    }
}