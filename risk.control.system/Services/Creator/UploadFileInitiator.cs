using System.Text;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Api;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IUploadFileInitiator
    {
        Task StartProcess(ApplicationUser companyUser, List<UploadCase> validRecords, byte[] zipFileByteData, FileOnFileSystemModel uploadFileData, int totalClaimsCreated, string url, bool uploadAndAssign = false);
    }

    internal class UploadFileInitiator : IUploadFileInitiator
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IFileUploadProcessor processor;
        private readonly IUploadFileStatusService uploadFileStatusService;
        private readonly ILogger<UploadFileInitiator> logger;
        private readonly IInvestigationService investigationService;
        private readonly IMailService mailService;
        private readonly IUploadService uploadService;

        public UploadFileInitiator(IDbContextFactory<ApplicationDbContext> contextFactory,
            IFileUploadProcessor processor,
            IUploadFileStatusService uploadFileStatusService,
            ILogger<UploadFileInitiator> logger,
            IInvestigationService investigationService,
            IMailService mailService,
            IUploadService uploadService)
        {
            _contextFactory = contextFactory;
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
                var uploadedCaseResult = await uploadService.FileUploadAsync(companyUser, validRecords, zipFileByteData, uploadFileData.FileOrFtp);
                if (uploadedCaseResult == null || uploadedCaseResult.Count == 0)
                {
                    await uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                }

                var uploadedCases = uploadedCaseResult.Where(c => c.InvestigationTask != null).Select(c => c.InvestigationTask).ToList();
                if (uploadedCases == null || uploadedCases.Count == 0)
                {
                    var errorString = "No valid records to upload.";
                    uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                    await uploadFileStatusService.SetFileUploadFailure(uploadFileData, errorString, uploadAndAssign);
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                }

                var totalAddedAndExistingCount = uploadedCaseResult.Count + totalClaimsCreated;
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial && totalAddedAndExistingCount > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    await uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"Case limit exceeded of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s).", uploadAndAssign);
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }
                var sb = new StringBuilder();

                if (uploadedCaseResult.Count > 0)
                {
                    var rowNum = 0;
                    sb.AppendLine("Row #, [FieldName = Error detail]"); // CSV header
                    var filteredErrors = uploadedCaseResult
                        .Select(r => r.Errors)
                        .Where(e => e.Count > 0);

                    foreach (var caseErrors in filteredErrors)
                    {
                        ++rowNum;
                        sb.AppendLine($"\"{rowNum}\", {string.Join(",", caseErrors)}");
                    }

                    if (rowNum > 0)
                    {
                        uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(sb.ToString());
                        await uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                        await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                        return;
                    }
                }

                if (uploadedCases == null || uploadedCases.Count == 0)
                {
                    await uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }

                var totalReadyToAssign = await investigationService.GetAutoCount(companyUser.Email);
                if (uploadedCases.Count + totalReadyToAssign > companyUser.ClientCompany.TotalToAssignMaxAllowed)
                {
                    await uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"Max count of {companyUser.ClientCompany.TotalToAssignMaxAllowed} Assign-Ready Case(s) limit reached.", uploadAndAssign, uploadedCases.Select(c => c.Id).ToList());
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }

                await using var _context = await _contextFactory.CreateDbContextAsync();
                _context.Investigations.AddRange(uploadedCases);

                await _context.SaveChangesAsync();

                await processor.ProcessloadFile(companyUser.Email, uploadedCases, uploadFileData, url, uploadAndAssign);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {UserEmail}", companyUser.Email);
                await uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
            }
        }
    }
}