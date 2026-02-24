using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Creator
{
    public interface IUploadFileStatusService
    {
        Task SetUploadAssignSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims, List<long> autoAllocated);

        Task SetUploadSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims);

        Task SetFileUploadFailure(FileOnFileSystemModel fileData, string message, bool uploadAndAssign, List<long> claimsIds = null);
    }

    internal class UploadFileStatusService : IUploadFileStatusService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public UploadFileStatusService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task SetUploadAssignSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims, List<long> autoAllocated)
        {
            var uploadedClaimCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.CLAIM);

            var assignedClaimCount = claims.Count(c => autoAllocated.Contains(c.Id) && c.PolicyDetail.InsuranceType == InsuranceType.CLAIM);

            var uploadedUnderWritingCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING);
            var assignedUnderWritingCount = claims.Count(c => autoAllocated.Contains(c.Id) && c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING);

            string message = $"Claims (Uploaded/Assigned) = ({uploadedClaimCount}/{assignedClaimCount}): Underwritings (Uploaded/Assigned) = ({uploadedUnderWritingCount}/{assignedUnderWritingCount})";
            fileData.Completed = true;
            fileData.Icon = "fas fa-check-circle i-green";
            fileData.Status = "Completed";
            fileData.Message = message;
            fileData.DirectAssign = true;
            fileData.RecordCount = claims.Count;
            fileData.CaseIds = claims.Select(c => new CaseListModel { CaseId = c.Id }).ToList();
            fileData.CompletedOn = DateTime.UtcNow;
            var timeTaken = DateTime.UtcNow.Subtract(fileData.CreatedOn).Seconds;
            fileData.TimeTakenSeconds = timeTaken == 0 ? 1 : timeTaken;
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.FilesOnFileSystem.Update(fileData);
            await context.SaveChangesAsync();
        }

        public async Task SetUploadSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims)
        {
            var claimCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.CLAIM);
            var underWritingCount = claims.Count(c => c.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING);

            string message = $"Uploaded Claims: {claimCount} & Underwritings: {underWritingCount}";
            fileData.Completed = true;
            fileData.Icon = "fas fa-check-circle i-green";
            fileData.Status = "Completed";
            fileData.Message = message;
            fileData.RecordCount = claims.Count;
            fileData.CaseIds = claims.Select(c => new CaseListModel { CaseId = c.Id }).ToList();
            fileData.CompletedOn = DateTime.UtcNow;
            var timeTaken = DateTime.UtcNow.Subtract(fileData.CreatedOn).Seconds;
            fileData.TimeTakenSeconds = timeTaken == 0 ? 1 : timeTaken;
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.FilesOnFileSystem.Update(fileData);
            await context.SaveChangesAsync();
        }

        public async Task SetFileUploadFailure(FileOnFileSystemModel fileData, string message, bool uploadAndAssign, List<long> claimsIds = null)
        {
            fileData.Completed = false;
            fileData.Icon = "fas fa-times-circle i-orangered";
            fileData.Status = "Error";
            fileData.Message = message;
            fileData.RecordCount = claimsIds == null ? 0 : claimsIds.Count;
            fileData.DirectAssign = uploadAndAssign;
            fileData.CompletedOn = DateTime.UtcNow;
            fileData.CaseIds = claimsIds?.Select(c => new CaseListModel { CaseId = c }).ToList();
            var timeTaken = DateTime.UtcNow.Subtract(fileData.CreatedOn).Seconds;
            fileData.TimeTakenSeconds = timeTaken == 0 ? 1 : timeTaken;
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.FilesOnFileSystem.Update(fileData);
            await context.SaveChangesAsync();
        }
    }
}