using System.Diagnostics;
using System.Text;
using Amazon.Runtime.Internal.Util;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IUploadFileInitiator
    {
        Task StartProcess(ApplicationUser companyUser, List<UploadCase> validRecords, byte[] zipFileByteData, FileOnFileSystemModel uploadFileData, int totalClaimsCreated, string url, bool uploadAndAssign = false);
    }
    internal class UploadFileInitiator: IUploadFileInitiator
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadProcessor processor;
        private readonly IUploadFileStatusService uploadFileStatusService;
        private readonly ILogger<UploadFileInitiator> logger;
        private readonly IInvestigationService investigationService;
        private readonly IMailService mailService;
        private readonly IUploadService uploadService;

        public UploadFileInitiator(ApplicationDbContext context,
            IFileUploadProcessor processor,
            IUploadFileStatusService uploadFileStatusService,
            ILogger<UploadFileInitiator> logger,
            IInvestigationService investigationService,
            IMailService mailService,
            IUploadService uploadService)
        {
            _context = context;
            this.processor = processor;
            this.uploadFileStatusService = uploadFileStatusService;
            this.logger = logger;
            this.investigationService = investigationService;
            this.mailService = mailService;
            this.uploadService = uploadService;
        }

        public async Task StartProcess(ApplicationUser companyUser, List<UploadCase> validRecords, byte[] zipFileByteData, FileOnFileSystemModel uploadFileData, int totalClaimsCreated, string url, bool uploadAndAssign = false)
        {
            try
            {
                var uploadedCaseResult = await uploadService.FileUpload(companyUser, validRecords, zipFileByteData, uploadFileData.FileOrFtp);
                var totalAddedAndExistingCount = uploadedCaseResult.Count + totalClaimsCreated;
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial && totalAddedAndExistingCount > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"Case limit exceeded of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s).", uploadAndAssign);
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }
                var sb = new StringBuilder();

                if (uploadedCaseResult.Count > 0)
                {
                    var rowNum = 0;
                    sb.AppendLine("Row #, [FieldName = Error detail]"); // CSV header
                    foreach (var caseErrors in from result in uploadedCaseResult
                                               let caseErrors = result.Errors
                                               where caseErrors.Count > 0
                                               select caseErrors)
                    {
                        ++rowNum;
                        sb.AppendLine($"\"{rowNum}\", {string.Join(",", caseErrors)}");
                    }

                    if (rowNum > 0)
                    {
                        uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                        uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(sb.ToString());
                        await _context.SaveChangesAsync();
                        await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                        return;
                    }
                }
                var uploadedCases = uploadedCaseResult.Select(c => c.InvestigationTask)?.ToList();

                if (uploadedCases == null || uploadedCases.Count == 0)
                {
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);

                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }
                

                var totalReadyToAssign = await investigationService.GetAutoCount(companyUser.Email);
                if (uploadedCases.Count + totalReadyToAssign > companyUser.ClientCompany.TotalToAssignMaxAllowed)
                {
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"Max count of {companyUser.ClientCompany.TotalToAssignMaxAllowed} Assign-Ready Case(s) limit reached.", uploadAndAssign, uploadedCases.Select(c => c.Id).ToList());
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }

                _context.Investigations.AddRange(uploadedCases);

                await _context.SaveChangesAsync();

                await processor.ProcessloadFile(companyUser.Email, uploadedCases, uploadFileData, companyUser, url, uploadAndAssign);

            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
            }
        }
    }
}
