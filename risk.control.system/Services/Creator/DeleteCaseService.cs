using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;

namespace risk.control.system.Services.Creator
{
    public interface IDeleteCaseService
    {
        Task<(bool Success, string Message)> SoftDeleteCaseAsync(long id, string currentUserEmail);

        Task<(bool Success, string Message)> SoftDeleteBulkCasesAsync(List<long> ids, string currentUserEmail);
    }

    internal class DeleteCaseService(ApplicationDbContext context, IAmazonApiService amazonApiService, IFeatureManager featureManager) : IDeleteCaseService
    {
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly ApplicationDbContext _context = context;

        public async Task<(bool Success, string Message)> SoftDeleteCaseAsync(long id, string currentUserEmail)
        {
            if (id <= 0) return (false, "Invalid ID provided.");

            var investigation = await _context.Investigations.Include(c => c.CustomerDetail).Include(b => b.BeneficiaryDetail).FirstOrDefaultAsync(i => i.Id == id);
            if (investigation == null) return (false, "Case not found.");

            ApplySoftDelete(investigation, currentUserEmail);
            if (await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
            {
                var imageCollection = EnvHelper.Get(CONSTANTS.FaceImageCollection);
                var awsIds = new List<string>();
                var customerAwsId = investigation.CustomerDetail!.AwsFaceId;
                if (!string.IsNullOrWhiteSpace(customerAwsId))
                {
                    awsIds.Add(customerAwsId);
                }
                var beneficiaryAwsId = investigation.BeneficiaryDetail!.AwsFaceId;
                if (!string.IsNullOrWhiteSpace(beneficiaryAwsId))
                {
                    awsIds.Add(beneficiaryAwsId);
                }
                if (awsIds.Count > 0)
                {
                    await _amazonApiService.DeleteFacesAsync(imageCollection!, awsIds);
                }
            }
            var deleted = await _context.SaveChangesAsync() > 0;
            return deleted ? (deleted, "Case deleted successfully!") : (deleted, "Failed to delete");
        }

        public async Task<(bool Success, string Message)> SoftDeleteBulkCasesAsync(List<long> ids, string currentUserEmail)
        {
            if (ids?.Any() != true) return (false, "No cases selected.");

            // Fetch all cases in one query for better performance
            var investigations = await _context.Investigations.Include(c => c.CustomerDetail).Include(b => b.BeneficiaryDetail)
                .Where(i => ids.Contains(i.Id) && !i.Deleted)
                .ToListAsync();

            if (investigations.Count == 0) return (false, "No valid cases found for deletion.");
            var imageCollection = EnvHelper.Get(CONSTANTS.FaceImageCollection);
            var awsIds = new List<string>();
            foreach (var investigation in investigations)
            {
                ApplySoftDelete(investigation, currentUserEmail);
                if (await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
                {
                    var customerAwsId = investigation.CustomerDetail!.AwsFaceId;
                    if (!string.IsNullOrWhiteSpace(customerAwsId))
                    {
                        awsIds.Add(customerAwsId);
                    }
                    var beneficiaryAwsId = investigation.BeneficiaryDetail!.AwsFaceId;
                    if (!string.IsNullOrWhiteSpace(beneficiaryAwsId))
                    {
                        awsIds.Add(beneficiaryAwsId);
                    }
                }
                if (awsIds.Count > 0)
                {
                    await _amazonApiService.DeleteFacesAsync(imageCollection!, awsIds);
                }
            }
            var deleted = await _context.SaveChangesAsync() > 0;
            return deleted ? (deleted, "Cases deleted successfully!") : (deleted, "Failed to delete");
        }

        private void ApplySoftDelete(InvestigationTask investigation, string userEmail)
        {
            investigation.Updated = DateTime.UtcNow;
            investigation.UpdatedBy = userEmail;
            investigation.Deleted = true;
            _context.Investigations.Update(investigation);
        }
    }
}