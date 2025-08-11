using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICaseCreationService
    {
        Task<UploadResult> FileUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, FileOnFileSystemModel model);
    }
    public class CaseCreationService : ICaseCreationService
    {

        private readonly ICaseDetailCreationService caseDetailCreationService;
        private readonly ILogger<CaseCreationService> logger;

        public CaseCreationService(ICaseDetailCreationService caseDetailCreationService, ILogger<CaseCreationService> logger)
        {
            this.caseDetailCreationService = caseDetailCreationService;
            this.logger = logger;
        }

        public async Task<UploadResult> FileUpload(ClientCompanyApplicationUser companyUser, UploadCase uploadCase, FileOnFileSystemModel model)
        {
            try
            {
                if (companyUser is null || uploadCase is null || model is null)
                {
                    logger.LogCritical($"Either company User and/or Upload case is null or No case saved");
                    return null;
                }
                var claimUploaded = await caseDetailCreationService.AddCaseDetail(uploadCase, companyUser, model);
                if (claimUploaded == null)
                {
                    logger.LogCritical($"Upload case(s) is null.");
                    return null;
                }
                return claimUploaded;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }
    }
}
