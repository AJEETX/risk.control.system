using System.Text;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IUploadFileDataProcessor
    {
        Task Process(ApplicationUser companyUser, List<UploadCase> validRecords, int totalClaimsCreated, FileOnFileSystemModel uploadFileData, string url, List<string> errors, bool uploadAndAssign = false);
    }

    internal class UploadFileDataProcessor(ApplicationDbContext context,
        IUploadFileStatusService uploadFileStatusService,
        ILogger<UploadFileDataProcessor> logger,
        ICaseNotificationService caseNotificationService) : IUploadFileDataProcessor
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IUploadFileStatusService _uploadFileStatusService = uploadFileStatusService;
        private readonly ILogger<UploadFileDataProcessor> _logger = logger;
        private readonly ICaseNotificationService _caseNotificationService = caseNotificationService;

        public async Task Process(ApplicationUser companyUser, List<UploadCase> validRecords, int totalClaimsCreated, FileOnFileSystemModel uploadFileData, string url, List<string> errors, bool uploadAndAssign = false)
        {
            try
            {
                if (companyUser.ClientCompany!.LicenseType == LicenseType.Trial && totalClaimsCreated > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    string errorString = $"Case limit reached of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s)."; // or use "," or "\n"
                    await _uploadFileStatusService.SetFileUploadFailure(uploadFileData, errorString, uploadAndAssign);
                    await _context.SaveChangesAsync();
                    await _caseNotificationService.NotifyFileUpload(companyUser.Email!, uploadFileData, url);
                    return;
                }

                if (errors.Count != 0)
                {
                    string errorString = string.Join(Environment.NewLine, errors); // or use "," or "\n"
                    uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                    await _uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"{errorString}", uploadAndAssign);
                    await _context.SaveChangesAsync();
                    await _caseNotificationService.NotifyFileUpload(companyUser.Email!, uploadFileData, url);
                    return;
                }

                if (validRecords.Count == 0)
                {
                    const string errorString = "No data";
                    uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                    await _uploadFileStatusService.SetFileUploadFailure(uploadFileData, errorString, uploadAndAssign);
                    await _context.SaveChangesAsync();
                    await _caseNotificationService.NotifyFileUpload(companyUser.Email!, uploadFileData, url);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded file for {UserEmail}", companyUser.Email);
            }
        }
    }
}