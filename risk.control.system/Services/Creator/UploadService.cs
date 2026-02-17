using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Creator
{
    public interface IUploadService
    {
        Task<IReadOnlyList<UploadResult>> FileUploadAsync(ApplicationUser companyUser, IReadOnlyList<UploadCase> uploadCases, byte[] model, ORIGIN source);
    }

    internal class UploadService : IUploadService
    {
        private readonly ICaseDetailCreationService _caseCreationService;
        private readonly ILogger<UploadService> logger;

        public UploadService(ICaseDetailCreationService caseCreationService,
            ILogger<UploadService> logger)
        {
            _caseCreationService = caseCreationService;
            this.logger = logger;
        }

        public async Task<IReadOnlyList<UploadResult>> FileUploadAsync(ApplicationUser companyUser, IReadOnlyList<UploadCase> uploadCases, byte[] model, ORIGIN source)
        {
            if (uploadCases == null || uploadCases.Count == 0)
            {
                logger.LogWarning("No upload data provided for user {Email}", companyUser.Email);
                return Array.Empty<UploadResult>();
            }

            var results = new List<UploadResult>(uploadCases.Count);

            for (int i = 0; i < uploadCases.Count; i++)
            {
                var row = uploadCases[i];

                try
                {
                    var result = await _caseCreationService.AddCaseDetail(row, companyUser, model, source);

                    if (result == null)
                    {
                        logger.LogWarning("Upload failed for row {RowNumber} (User: {Email})", i + 1, companyUser.Email);

                        continue;
                    }

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error uploading row {RowNumber} for user {Email}", i + 1, companyUser.Email);
                }
            }
            return results;
        }
    }
}