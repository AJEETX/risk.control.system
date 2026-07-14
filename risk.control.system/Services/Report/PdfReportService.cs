using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Model;
using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Agentic;
using risk.control.system.Services.Common;
namespace risk.control.system.Services.Report
{
    public interface IPdfReportService
    {
        Task<string> Generate(long investigationTaskId, string userEmail);
        Task GenerateAgencyReport(long investigationTaskId);
    }

    internal class PdfReportService(
        IAmazonS3 s3Client,
        ApplicationDbContext context,
        IWebHostEnvironment env,
        IPdfGenerateReportService pdfGenerate,
        IFileStorageService fileStorageService,
        IAgenticService agenticService,
        IPdfGenerateCaseDetailService caseDetailService,
        ILogger<PdfReportService> logger,
        IPdfGenerateDetailReportService pdfGenerateDetail) : IPdfReportService
    {
        private const string reportFilename = "Agency_Report.pdf";
        private const string zipFilename = "Agency_Report.zip";
        private const string zipFolderName = "Report";
        private const string ClaimFormName = "Claim_Form.jpg";
        private const string UnderwritingFormName = "Underwriting_Form.jpg";
        private const string extension = ".jpg";
        private readonly string bucketName = CONSTANTS.S3_BUCKET;
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly IPdfGenerateReportService _pdfGenerate = pdfGenerate;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly IAgenticService _agenticService = agenticService;
        private readonly IPdfGenerateDetailReportService _pdfGenerateDetail = pdfGenerateDetail;
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly IPdfGenerateCaseDetailService _caseDetailService = caseDetailService;
        private readonly ILogger<PdfReportService> _logger = logger;
        [AutomaticRetry(Attempts = 0)]
        public async Task<string> Generate(long investigationTaskId, string userEmail)
        {
            try
            {
                var investigation = _context.Investigations.Include(c => c.CustomerDetail).Include(c => c.BeneficiaryDetail).Include(c => c.ClientCompany)
                        .ThenInclude(c => c!.Country).Include(c => c.PolicyDetail).Include(c => c.InvestigationReport).ThenInclude(c => c!.EnquiryRequest).Include(c => c.InvestigationReport).ThenInclude(c => c!.EnquiryRequests)
                    .FirstOrDefault(c => c.Id == investigationTaskId);
                var policy = _context.PolicyDetail.Include(p => p.CaseEnabler).Include(p => p.CostCentre).Include(p => p.InvestigationServiceType)
                    .FirstOrDefault(p => p.PolicyDetailId == investigation!.PolicyDetail!.PolicyDetailId);
                var customer = _context.CustomerDetail.Include(c => c.District).Include(c => c.State).Include(c => c.Country).Include(c => c.PinCode)
                    .FirstOrDefault(c => c.InvestigationTaskId == investigationTaskId);
                var beneficiary = _context.BeneficiaryDetail.Include(b => b.District).Include(b => b.State).Include(b => b.Country).Include(b => b.PinCode).Include(b => b.BeneficiaryRelation)
                    .FirstOrDefault(b => b.InvestigationTaskId == investigationTaskId);
                var investigationReport = await _context.ReportTemplates.Include(r => r.LocationReport).ThenInclude(l => l.AgentIdReport)
                   .Include(r => r.LocationReport).ThenInclude(l => l.FaceIds).Include(r => r.LocationReport).ThenInclude(l => l.DocumentIds)
                   .Include(r => r.LocationReport).ThenInclude(l => l.Questions).FirstOrDefaultAsync(q => q.Id == investigation!.ReportTemplateId);
                var vendor = _context.Vendor.Include(s => s.District).Include(s => s.State).Include(s => s.Country).Include(s => s.PinCode).Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == investigation!.VendorId);
                var currentUser = _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
                var investigationServiced = vendor!.VendorInvestigationServiceTypes!.FirstOrDefault(s => s.InvestigationServiceTypeId == policy!.InvestigationServiceTypeId);
                investigationServiced ??= vendor.VendorInvestigationServiceTypes!.FirstOrDefault();
                var investigatService = _context.InvestigationServiceType.FirstOrDefault(i => i.InvestigationServiceTypeId == policy!.InvestigationServiceTypeId);
                var invoice = new VendorInvoice
                {
                    ClientCompanyId = currentUser!.ClientCompany!.ClientCompanyId,
                    GrandTotal = investigationServiced!.Price + (investigationServiced.Price * (1m / 10m)),
                    NoteToRecipient = "Auto generated Invoice",
                    Updated = DateTime.UtcNow,
                    ClientCompany = currentUser.ClientCompany,
                    UpdatedBy = userEmail,
                    VendorId = vendor.VendorId,
                    InvestigationReportId = investigation!.InvestigationReport?.Id,
                    SubTotal = investigationServiced.Price,
                    TaxAmount = investigationServiced.Price * (1m / 10m),
                    InvestigationServiceType = investigatService,
                    CaseId = investigationTaskId,
                    Currency = CustomExtensions.GetCultureByCountry(investigation.ClientCompany!.Country!.Code.ToUpper()).NumberFormat.CurrencySymbol
                };
                _context.VendorInvoice.Add(invoice);
                await _context.SaveChangesAsync(null, false);
                var reportFilename = await _pdfGenerate.BuildInvestigationPdfReport(investigation, policy!, customer!, beneficiary!, investigationReport!, vendor);
                _context.Investigations.Update(investigation);
                await _context.SaveChangesAsync(null, false);
                return policy!.ContractNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred  while generating PDF report for InvestigationTaskId: {InvestigationTaskId}", investigationTaskId);
                throw; // Rethrow the exception to be handled by Hangfire or the calling method
            }
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task GenerateAgencyReport(long investigationTaskId)
        {
            var investigation = _context.Investigations.Include(c => c.CustomerDetail).Include(c => c.BeneficiaryDetail).Include(c => c.ClientCompany)
                    .ThenInclude(c => c!.Country).Include(c => c.PolicyDetail).Include(c => c.InvestigationReport).ThenInclude(c => c!.EnquiryRequest).Include(c => c.InvestigationReport).ThenInclude(c => c!.EnquiryRequests)
                .FirstOrDefault(c => c.Id == investigationTaskId);

            var customer = _context.CustomerDetail.Include(c => c.District).Include(c => c.State).Include(c => c.Country).Include(c => c.PinCode)
                    .FirstOrDefault(c => c.InvestigationTaskId == investigationTaskId);
            var beneficiary = _context.BeneficiaryDetail.Include(b => b.District).Include(b => b.State).Include(b => b.Country).Include(b => b.PinCode).Include(b => b.BeneficiaryRelation)
                .FirstOrDefault(b => b.InvestigationTaskId == investigationTaskId);

            var policy = _context.PolicyDetail.Include(p => p.CaseEnabler).Include(p => p.CostCentre).Include(p => p.InvestigationServiceType)
                .FirstOrDefault(p => p.PolicyDetailId == investigation!.PolicyDetail!.PolicyDetailId);

            var investigationReport = await _context.ReportTemplates.Include(r => r.LocationReport).ThenInclude(l => l.AgentIdReport)
               .Include(r => r.LocationReport).ThenInclude(l => l.FaceIds).Include(r => r.LocationReport).ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationReport).ThenInclude(l => l.Questions).FirstOrDefaultAsync(q => q.Id == investigation!.ReportTemplateId);

            var vendor = _context.Vendor.Include(s => s.District).Include(s => s.State).Include(s => s.Country).Include(s => s.PinCode).Include(s => s.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == investigation!.VendorId);

            DocumentBuilder builder = DocumentBuilder.New();
            SectionBuilder section = builder.AddSection();
            section.SetOrientation(PageOrientation.Landscape);
            bool isClaim = true;
            if (policy!.InsuranceType == InsuranceType.UNDERWRITING)
            {
                isClaim = false;
                section = _caseDetailService.BuildUnderwritng(section, investigation!, policy, customer!, beneficiary!);
            }
            else
            {
                section = _caseDetailService.BuildClaim(section, investigation!, policy, customer!, beneficiary!);
            }
            section.AddParagraph().SetMarginBottom(10f);

            section = await _pdfGenerateDetail.Build(section, investigation!, investigationReport!, vendor!, isClaim);

            string agencyReportFilePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.DOCUMENT, CONSTANTS.CASE, policy.ContractNumber, zipFolderName, reportFilename));
            builder.Build(agencyReportFilePath);

            var policyDocument = await File.ReadAllBytesAsync(policy.DocumentPath!);
            var fileName = isClaim ? ClaimFormName : UnderwritingFormName;
            var allowedExtensions = new[] { extension };
            var (_, _) = await _fileStorageService.SaveAsync(policyDocument, extension, CONSTANTS.CASE, policy.ContractNumber, zipFolderName, allowedExtensions, fileName);

            var sourceFolderPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.DOCUMENT, CONSTANTS.CASE, policy.ContractNumber, zipFolderName));
            var s3KeyName = $"backups/{policy.ContractNumber}/{zipFilename}";
            await ZipAndUploadToS3Async(sourceFolderPath, s3KeyName);
        }
        private async Task<bool> ZipAndUploadToS3Async(string sourceFolder, string s3Key)
        {
            try
            {
                if (!(await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName)))
                {
                    var putBucketRequest = new PutBucketRequest { BucketName = bucketName, UseClientRegion = true };

                    await _s3Client.PutBucketAsync(putBucketRequest);

                    var publicAccessBlockRequest = new PutPublicAccessBlockRequest
                    {
                        BucketName = bucketName,
                        PublicAccessBlockConfiguration = new PublicAccessBlockConfiguration
                        {
                            BlockPublicAcls = true,
                            BlockPublicPolicy = true,
                            IgnorePublicAcls = true,
                            RestrictPublicBuckets = true
                        }
                    };
                    await _s3Client.PutPublicAccessBlockAsync(publicAccessBlockRequest);
                }

                await using var memoryStream = new MemoryStream();

                ZipFile.CreateFromDirectory(sourceFolder, memoryStream, CompressionLevel.Optimal, includeBaseDirectory: false);
                memoryStream.Position = 0;

                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = s3Key,
                    InputStream = memoryStream,
                    ContentType = "application/zip",
                };

                PutObjectResponse response = await _s3Client.PutObjectAsync(putRequest);
                return (response.HttpStatusCode == System.Net.HttpStatusCode.OK);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // The object already exists, handle accordingly
                _logger.LogError(ex, "Error occurred while zipping and uploading to S3.");
                return false;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                _logger.LogError(ex, "Error occurred while zipping and uploading to S3.");
                return false;
            }
        }
    }
}