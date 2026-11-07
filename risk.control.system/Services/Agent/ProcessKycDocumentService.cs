using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agent
{
    public interface IProcessKycDocumentService
    {
        Task ProcessOcrResults(DocumentIdReport doc, byte[] docImage, IReadOnlyList<TextBlock> ocrResult, InvestigationTask caseDetail, string extension, string docFullName, int pageNumber = 1);
    }
    internal class ProcessKycDocumentService(ApplicationDbContext context,
    IProcessImageService processImageService,
    IFileStorageService fileStorageService,
    ILogger<ProcessKycDocumentService> logger,
    IAdharCardService adharCardService,
    IDeathCertificateService deathCertificateService,
    IIncomeTaxService incomeTaxService,
    IWebHostEnvironment env,
    IPanCardService panCardService) : IProcessKycDocumentService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly ILogger<ProcessKycDocumentService> _logger = logger;
        private readonly IAdharCardService _adharCardService = adharCardService;
        private readonly IPanCardService _panCardService = panCardService;
        private readonly IDeathCertificateService _deathCertificateService = deathCertificateService;
        private readonly IIncomeTaxService _incomeTaxService = incomeTaxService;
        private readonly IWebHostEnvironment _env = env;
        public async Task ProcessOcrResults(DocumentIdReport doc, byte[] docImage, IReadOnlyList<TextBlock> ocrResult, InvestigationTask caseDetail, string extension, string docFullName, int pageNumber = 1)
        {
            try
            {
                if (ocrResult?.Count > 0)
                {
                    var company = await _context.ClientCompany.FindAsync(caseDetail.ClientCompanyId);
                    var originalPath = doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == pageNumber)?.OriginalFilePath;
                    if (doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName())
                    {
                        var isUnderwriting = caseDetail.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING;
                        string registeredImagePath = isUnderwriting ? caseDetail.CustomerDetail!.ImagePath! : caseDetail.BeneficiaryDetail!.ImagePath!;
                        var regPath = Path.Combine(_env.ContentRootPath, FaceIdfyHelper.GetRegisteredImagePath(caseDetail, isUnderwriting));
                        var registeredImageBytes = await File.ReadAllBytesAsync(regPath);

                        var maskedDocImage = await _panCardService.Process(docImage, registeredImageBytes, ocrResult, company!, doc, pageNumber);
                        var (_, originalRelativePath) = await _fileStorageService.SaveAsync(maskedDocImage, extension, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT, null, $"{docFullName}{extension}");
                        doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == pageNumber)!.OriginalFilePath = originalRelativePath;
                    }
                    else if (doc.ReportName == DocumentIdReportType.ADHAAR.GetEnumDisplayName())
                    {
                        var isUnderwriting = caseDetail.PolicyDetail?.InsuranceType == InsuranceType.UNDERWRITING;
                        string registeredImagePath = isUnderwriting ? caseDetail.CustomerDetail!.ImagePath! : caseDetail.BeneficiaryDetail!.ImagePath!;
                        var regPath = Path.Combine(_env.ContentRootPath, FaceIdfyHelper.GetRegisteredImagePath(caseDetail, isUnderwriting));
                        var registeredImageBytes = await File.ReadAllBytesAsync(regPath);

                        var maskedDocImage = await _adharCardService.Process(docImage, registeredImageBytes, ocrResult, company!, doc, pageNumber);
                        var (_, originalRelativePath) = await _fileStorageService.SaveAsync(maskedDocImage, extension, CONSTANTS.CASE, caseDetail.PolicyDetail!.ContractNumber, CONSTANTS.REPORT, null, $"{docFullName}_{pageNumber}{extension}");
                        doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == pageNumber)!.OriginalFilePath = originalRelativePath;
                    }
                    else
                    {
                        var compressed = _processImageService.CompressAndWaterMarkImage(docImage, 80, "VALID", 0F);
                        await File.WriteAllBytesAsync(doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == pageNumber)!.FilePath!, compressed);
                        doc.ImageValid = true;
                        doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == pageNumber)!.OcrText = ocrResult.FirstOrDefault()?.Text ?? string.Empty;
                    }
                }
                else
                {
                    doc.ImageValid = false;
                    doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == pageNumber)!.OcrText = "No OCR data detected";
                    await File.WriteAllBytesAsync(doc.DocumentFiles.FirstOrDefault(f => f.PageNumber == pageNumber)!.FilePath!, _processImageService.CompressAndWaterMarkImage(docImage, 80, "UNKNOWN", 2F));
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
