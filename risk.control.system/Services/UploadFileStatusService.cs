using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IUploadFileStatusService
    {
        void SetUploadAssignSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims, List<long> autoAllocated);

        void SetUploadSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims);

        void SetFileUploadFailure(FileOnFileSystemModel fileData, string message, bool uploadAndAssign, List<long> claimsIds = null);
    }

    internal class UploadFileStatusService : IUploadFileStatusService
    {
        private readonly ApplicationDbContext context;

        public UploadFileStatusService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public void SetUploadAssignSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims, List<long> autoAllocated)
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
            fileData.CompletedOn = DateTime.Now;
            var timeTaken = DateTime.Now.Subtract(fileData.CreatedOn).Seconds;
            fileData.TimeTakenSeconds = timeTaken == 0 ? 1 : timeTaken;
            context.FilesOnFileSystem.Update(fileData);
            context.SaveChanges();
        }

        public void SetUploadSuccess(FileOnFileSystemModel fileData, List<InvestigationTask> claims)
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
            fileData.CompletedOn = DateTime.Now;
            var timeTaken = DateTime.Now.Subtract(fileData.CreatedOn).Seconds;
            fileData.TimeTakenSeconds = timeTaken == 0 ? 1 : timeTaken;
            context.FilesOnFileSystem.Update(fileData);
            context.SaveChanges();
        }

        public void SetFileUploadFailure(FileOnFileSystemModel fileData, string message, bool uploadAndAssign, List<long> claimsIds = null)
        {
            fileData.Completed = false;
            fileData.Icon = "fas fa-times-circle i-orangered";
            fileData.Status = "Error";
            fileData.Message = message;
            fileData.RecordCount = claimsIds == null ? 0 : claimsIds.Count;
            fileData.DirectAssign = uploadAndAssign;
            fileData.CompletedOn = DateTime.Now;
            fileData.CaseIds = claimsIds?.Select(c => new CaseListModel { CaseId = c }).ToList();
            var timeTaken = DateTime.Now.Subtract(fileData.CreatedOn).Seconds;
            fileData.TimeTakenSeconds = timeTaken == 0 ? 1 : timeTaken;
            context.FilesOnFileSystem.Update(fileData);
            context.SaveChanges();
        }
    }
}