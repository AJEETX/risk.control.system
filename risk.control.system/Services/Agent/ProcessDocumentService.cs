using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent
{
    public interface IProcessDocumentService
    {
        Task ProcessOcrResults(DocumentIdReport doc, byte[] docImage, IReadOnlyList<TextBlock> ocrResult, InvestigationTask caseDetail, string extension);
    }
    internal class ProcessDocumentService(ApplicationDbContext context,
    IProcessImageService processImageService,
    IFileStorageService fileStorageService,
    ILogger<ProcessDocumentService> logger,
    IAdharCardService adharCardService,
    IPanCardService panCardService) : IProcessDocumentService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly ILogger<ProcessDocumentService> _logger = logger;
        private readonly IAdharCardService _adharCardService = adharCardService;
        private readonly IPanCardService _panCardService = panCardService;
        public async Task ProcessOcrResults(DocumentIdReport doc, byte[] docImage, IReadOnlyList<TextBlock> ocrResult, InvestigationTask caseDetail, string extension)
        {
            try
            {
                if (ocrResult?.Count > 0)
                {
                    var company = await _context.ClientCompany.FindAsync(caseDetail.ClientCompanyId);

                    if (doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName())
                    {
                        var maskedDocImage = await _panCardService.Process(docImage, ocrResult, company!, doc);
                        var (_, originalRelativePath) = await _fileStorageService.SaveAsync(maskedDocImage, extension, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT, null, $"{doc.ReportName}{extension}");
                        doc.OriginalFilePath = originalRelativePath;
                    }
                    else if (doc.ReportName == DocumentIdReportType.ADHAAR.GetEnumDisplayName())
                    {
                        var maskedDocImage = await _adharCardService.Process(docImage, ocrResult, company!, doc);
                        var (_, originalRelativePath) = await _fileStorageService.SaveAsync(maskedDocImage, extension, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT, null, $"{doc.ReportName}{extension}");
                        doc.OriginalFilePath = originalRelativePath;
                    }
                    else
                    {
                        var compressed = _processImageService.CompressImage(docImage);
                        await File.WriteAllBytesAsync(doc.FilePath!, compressed);
                        doc.ImageValid = true;
                        doc.LocationInfo = ocrResult.FirstOrDefault()?.Text ?? string.Empty;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed Document file processing for Case {CaseId}", caseDetail.Id);
                return;
            }
        }
    }
}
