using Google.Cloud.Vision.V1;

using Microsoft.EntityFrameworkCore;
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

internal class DocumentIdfyService : IDocumentIdfyService
{
    private readonly ApplicationDbContext context;
    private readonly IAgentCaseDetailService caseService;
    private readonly IProcessImageService processImageService;
    private readonly ILogger<FaceIdfyService> logger;
    private readonly IFileStorageService fileStorageService;
    private readonly IPanCardService panCardService;
    private readonly IGoogleService googleApi;
    private readonly IHttpClientService httpClientService;
    private readonly ICustomApiClient customApiCLient;

    public DocumentIdfyService(ApplicationDbContext context,
        IAgentCaseDetailService caseService,
        IProcessImageService processImageService,
        ILogger<FaceIdfyService> logger,
        IFileStorageService fileStorageService,
        IPanCardService panCardService,
        IGoogleService googleApi,
        IHttpClientService httpClientService,
        ICustomApiClient customApiCLient)
    {
        this.context = context;
        this.caseService = caseService;
        this.processImageService = processImageService;
        this.logger = logger;
        this.fileStorageService = fileStorageService;
        this.panCardService = panCardService;
        this.googleApi = googleApi;
        this.httpClientService = httpClientService;
        this.customApiCLient = customApiCLient;
    }

    public async Task<AppiCheckifyResponse> CaptureDocumentId(DocumentData data)
    {
        var claim = await caseService.GetCaseById(data.CaseId);
        if (claim?.InvestigationReport == null) return null;

        var location = claim.InvestigationReport.ReportTemplate.LocationReport
            .FirstOrDefault(l => l.LocationName == data.LocationName);

        var locationTemplate = await context.LocationReport
            .Include(l => l.DocumentIds)
            .FirstOrDefaultAsync(l => l.Id == location.Id);

        var documentReport = locationTemplate.DocumentIds.FirstOrDefault(c => c.ReportName == data.ReportName);

        try
        {
            // 1. Data Preparation
            var (lat, lon) = VerificationHelper.ParseCoordinates(data.LocationLatLong);
            var expected = VerificationHelper.GetExpectedCoordinates(claim);
            var (fileName, relativePath) = await fileStorageService.SaveAsync(data.Image, "Case", claim.PolicyDetail.ContractNumber, "report");

            documentReport.FilePath = relativePath;
            documentReport.ImageExtension = Path.GetExtension(fileName);
            byte[] docImage = await VerificationHelper.GetBytesFromIFormFile(data.Image);

            // 2. Parallel Service Calls (OCR, Address, and Mapping)
            var googleTask = googleApi.DetectTextAsync(documentReport.FilePath);
            var addressTask = httpClientService.GetRawAddress(lat, lon);
            var mapTask = customApiCLient.GetMap(expected.lat, expected.lon, double.Parse(lat), double.Parse(lon), "Start", "End", "300", "300", "green", "red");

            await Task.WhenAll(googleTask, addressTask, mapTask);

            // 3. Process Results
            var (dist, distM, dur, durS, mapUrl) = await mapTask;
            documentReport.LocationMapUrl = mapUrl;
            documentReport.DistanceInMetres = distM;
            documentReport.DurationInSeconds = durS;
            documentReport.LocationAddress = await addressTask;
            documentReport.LongLat = $"Latitude = {lat}, Longitude = {lon}";
            documentReport.LongLatTime = DateTime.Now;

            var detectedText = await googleTask;
            await ProcessOcrResults(documentReport, docImage, detectedText, claim);

            // 4. Persistence
            locationTemplate.ValidationExecuted = true;
            locationTemplate.Updated = DateTime.Now;
            locationTemplate.UpdatedBy = data.Email;

            context.DocumentIdReport.Update(documentReport);
            context.Investigations.Update(claim);
            await context.SaveChangesAsync();

            return MapResponse(claim, documentReport, docImage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed Document file capture/processing for Case {CaseId}. {AgentEmail}", data.CaseId, data.Email);
            return await HandleError(claim, documentReport);
        }
    }

    private async Task ProcessOcrResults(DocumentIdReport doc, byte[] docImage, IReadOnlyList<EntityAnnotation> ocrResult, InvestigationTask claim)
    {
        if (ocrResult != null && ocrResult.Count > 0)
        {
            var company = await context.ClientCompany.FindAsync(claim.ClientCompanyId);

            if (doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName())
            {
                await panCardService.Process(docImage, ocrResult, company, doc, doc.ImageExtension);
            }
            else
            {
                var compressed = processImageService.ProcessCompress(docImage, doc.ImageExtension);
                await File.WriteAllBytesAsync(doc.FilePath, compressed);
                doc.ImageValid = true;
                doc.LocationInfo = ocrResult.FirstOrDefault()?.Description;
            }
        }
        else
        {
            doc.ImageValid = false;
            doc.LocationInfo = "No OCR data detected";
            await File.WriteAllBytesAsync(doc.FilePath, processImageService.ProcessCompress(docImage, doc.ImageExtension));
        }
        doc.ValidationExecuted = true;
    }

    private AppiCheckifyResponse MapResponse(InvestigationTask claim, DocumentIdReport doc, byte[] image)
    {
        return new AppiCheckifyResponse
        {
            BeneficiaryId = claim.BeneficiaryDetail.BeneficiaryDetailId,
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
        await context.SaveChangesAsync();

        var img = File.Exists(doc.FilePath) ? await File.ReadAllBytesAsync(doc.FilePath) : null;
        return MapResponse(claim, doc, img);
    }
}