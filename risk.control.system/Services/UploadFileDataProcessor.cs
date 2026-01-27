using System.Text;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IUploadFileDataProcessor
    {
        Task Process(ApplicationUser companyUser, List<UploadCase> validRecords, int totalClaimsCreated, FileOnFileSystemModel uploadFileData, string url, List<string> errors, bool uploadAndAssign = false);
    }
    internal class UploadFileDataProcessor: IUploadFileDataProcessor
    {
        private readonly ApplicationDbContext context;
        private readonly IUploadFileStatusService uploadFileStatusService;
        private readonly ILogger<UploadFileDataProcessor> logger;
        private readonly IMailService mailService;

        public UploadFileDataProcessor(ApplicationDbContext context,
            IUploadFileStatusService uploadFileStatusService,
            ILogger<UploadFileDataProcessor> logger,
            IMailService mailService)
        {
            this.context = context;
            this.uploadFileStatusService = uploadFileStatusService;
            this.logger = logger;
            this.mailService = mailService;
        }
        public async Task Process(ApplicationUser companyUser, List<UploadCase> validRecords, int totalClaimsCreated, FileOnFileSystemModel uploadFileData, string url, List<string> errors, bool uploadAndAssign = false)
        {
            try
            {
                if (companyUser.ClientCompany.LicenseType == LicenseType.Trial && totalClaimsCreated > companyUser.ClientCompany.TotalCreatedClaimAllowed)
                {
                    string errorString = $"Case limit reached of {companyUser.ClientCompany.TotalCreatedClaimAllowed} case(s)."; // or use "," or "\n"
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, errorString, uploadAndAssign);
                    await context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }

                if (errors.Any())
                {
                    string errorString = string.Join(Environment.NewLine, errors); // or use "," or "\n"
                    uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"{errorString}", uploadAndAssign);
                    await context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }

                if (validRecords.Count == 0)
                {
                    string errorString = "No data";
                    uploadFileData.ErrorByteData = Encoding.UTF8.GetBytes(errorString);
                    uploadFileStatusService.SetFileUploadFailure(uploadFileData, $"{errorString}", uploadAndAssign);
                    await context.SaveChangesAsync();
                    await mailService.NotifyFileUpload(companyUser.Email, uploadFileData, url);
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing uploaded file for {UserEmail}", companyUser.Email);
            }
            
        }
    }
}
