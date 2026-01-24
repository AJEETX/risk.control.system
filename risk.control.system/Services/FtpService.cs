using System.Data;
using System.Text;

using Hangfire;

using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using System.Linq;

namespace risk.control.system.Services
{
    public interface IFtpService
    {
        Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false);

    }

    internal class FtpService : IFtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUploadFileStatusService uploadFileStatusService;
        private readonly ICsvFileReaderService csvFileReaderService;
        private readonly ILogger<FtpService> logger;
        private readonly ITimelineService timelineService;
        private readonly IInvestigationService investigationService;
        private readonly IMailService mailService;
        private readonly IProcessCaseService processCaseService;
        private readonly IUploadService uploadService;

        public FtpService(ApplicationDbContext context,
            IUploadFileStatusService uploadFileStatusService,
            ICsvFileReaderService csvFileReaderService,
            ILogger<FtpService> logger,
            ITimelineService timelineService,
            IInvestigationService investigationService,
            IMailService mailService,
            IProcessCaseService processCaseService,
            IUploadService uploadService)
        {
            _context = context;
            this.uploadFileStatusService = uploadFileStatusService;
            this.csvFileReaderService = csvFileReaderService;
            this.logger = logger;
            this.timelineService = timelineService;
            this.investigationService = investigationService;
            this.mailService = mailService;
            this.processCaseService = processCaseService;
            this.uploadService = uploadService;
        }


        [AutomaticRetry(Attempts = 0)]
        public async Task StartFileUpload(string userEmail, int uploadId, string url, bool uploadAndAssign = false)
        {
            var companyUser = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);
            var uploadFileData = await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            
            var (zipFileByteData, validRecords, errors) =await csvFileReaderService.ReadPipeDelimitedCsvFromZip(uploadFileData); // Read the first CSV file from the ZIP archive
            var totalClaimsCreated = await _context.Investigations.CountAsync(c => !c.Deleted && c.ClientCompanyId == companyUser.ClientCompanyId);
            
            if (companyUser.ClientCompany.LicenseType == LicenseType.Trial && totalClaimsCreated > companyUser.ClientCompany.TotalCreatedClaimAllowed)
            {
                string errorString = $"Case limit reached of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s)."; // or use "," or "\n"
                uploadFileStatusService.SetFileUploadFailure(uploadFileData, errorString, uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                return;
            }

            if (errors.Any())
            {
                string errorString = string.Join(Environment.NewLine, errors); // or use "," or "\n"
                uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"{errorString}", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                return;
            }

            if (validRecords.Count == 0)
            {
                string errorString = "No data";
                uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"{errorString}", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                return;
            }

            try
            {
                var uploadedCaseResult = await uploadService.FileUpload(companyUser, validRecords, zipFileByteData, uploadFileData.FileOrFtp);
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
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                        return;
                    }
                }
                var uploadedCases = uploadedCaseResult.Select(c => c.InvestigationTask)?.ToList();

                if (uploadedCases == null || uploadedCases.Count == 0)
                {
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);

                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }
                var totalAddedAndExistingCount = uploadedCases.Count + totalClaimsCreated;
                // License Check (if Trial)
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial && totalAddedAndExistingCount > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"Case limit exceeded of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s).", uploadAndAssign);
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }

                var totalReadyToAssign = await investigationService.GetAutoCount(userEmail);
                if (uploadedCases.Count + totalReadyToAssign > companyUser.ClientCompany.TotalToAssignMaxAllowed)
                {
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"Max count of {companyUser.ClientCompany.TotalToAssignMaxAllowed} Assign-Ready Case(s) limit reached.", uploadAndAssign, uploadedCases.Select(c => c.Id).ToList());
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }

                _context.Investigations.AddRange(uploadedCases);

                await _context.SaveChangesAsync();

                try
                {
                    if (uploadAndAssign && uploadedCases.Any())
                    {
                        // Auto-Assign Claims if Enabled
                        var claimsIds = uploadedCases.Select(c => c.Id).ToList();
                        var autoAllocated = await processCaseService.BackgroundUploadAutoAllocation(claimsIds, userEmail, url);
                        uploadFileStatusService.SetUploadAssignSuccess(uploadFileData, uploadedCases, autoAllocated);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Upload Success
                        uploadFileStatusService.SetUploadSuccess(uploadFileData, uploadedCases);
                        await _context.SaveChangesAsync();

                        var updateTasks = uploadedCases.Select(u => timelineService.UpdateTaskStatus(u.Id, userEmail));
                        await Task.WhenAll(updateTasks);

                        // Notify User
                        await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    }
                    companyUser.ClientCompany.TotalCreatedClaimAllowed -= uploadedCases.Count;
                    await _context.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred");
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error Assigning cases", uploadAndAssign, uploadedCases.Select(u => u.Id).ToList());
                    await _context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
                    return;
                }
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred");
                uploadFileStatusService.SetFileUploadFailure(uploadFileData, "Error uploading the file", uploadAndAssign);
                await _context.SaveChangesAsync();
                await mailService.NotifyFileUpload(userEmail, uploadFileData, url);
            }
        }
    }
}