using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IUploadService
    {
        Task<List<UploadResult>> FileUpload(ApplicationUser companyUser, List<UploadCase> customData, byte[] model, ORIGIN fileOrFTP);
    }
    internal class UploadService : IUploadService
    {
        private readonly IProgressService uploadProgressService;
        private readonly ICaseCreationService _caseCreationService;
        private readonly ILogger<UploadService> logger;

        public UploadService(ICaseCreationService caseCreationService,
            ILogger<UploadService> logger,
            IProgressService uploadProgressService)
        {
            _caseCreationService = caseCreationService;
            this.logger = logger;
            this.uploadProgressService = uploadProgressService;
        }

        public async Task<List<UploadResult>> FileUpload(ApplicationUser companyUser, List<UploadCase> customData, byte[] model, ORIGIN fileOrFTP)
        {
            var uploadedClaims = new List<UploadResult>();
            try
            {
                if (customData == null || customData.Count == 0)
                {
                    return null; // Return 0 if no CSV data is found
                }
                var totalCount = customData.Count;
                foreach (var row in customData)
                {
                    var claimUploaded = await _caseCreationService.FileUpload(companyUser, row, model, fileOrFTP);
                    if (claimUploaded == null)
                    {
                        return null;
                    }
                    uploadedClaims.Add(claimUploaded);
                    //int progress = (int)(((uploadedRecordsCount + 1) / (double)totalCount) * 100);
                    //uploadProgressService.UpdateProgress(model.Id, progress);
                    //uploadedRecordsCount++;
                }
                return uploadedClaims;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                return uploadedClaims;
            }
        }
    }
}
