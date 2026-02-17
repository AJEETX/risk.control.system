using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface ICreatorDashboardService
    {
        Task<DashboardData> GetCreatorCount(string userEmail, string role);
    }

    internal class CreatorDashboardService : ICreatorDashboardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public CreatorDashboardService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<DashboardData> GetCreatorCount(string userEmail, string role)
        {
            // Always use the factory to get a fresh DbContext
            await using var db1 = _contextFactory.CreateDbContext();

            // Fetch the user
            var companyUser = await db1.ApplicationUser
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null || !companyUser.ClientCompanyId.HasValue)
                throw new InvalidOperationException($"User '{userEmail}' not found or missing ClientCompanyId.");

            var companyId = companyUser.ClientCompanyId.Value;

            // Fetch company info
            var companyTask = db1.ClientCompany
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyId);

            // Define substatus sets
            var activeExcludedSubStatuses = new[]
            {
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_IN_PROGRESS,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
            };

            var assignableSubStatuses = new[]
            {
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
            };

            // Fetch investigations and files concurrently
            await using var db2 = _contextFactory.CreateDbContext();
            var investigationsTask = db2.Investigations
                .AsNoTracking()
                .Where(c => c.ClientCompanyId == companyId && c.CreatedUser == userEmail && !c.Deleted)
                .Select(c => new
                {
                    InsuranceType = c.PolicyDetail != null ? c.PolicyDetail.InsuranceType : (InsuranceType?)null,
                    SubStatus = c.SubStatus,
                    Status = c.Status
                })
                .ToListAsync();

            await using var db3 = _contextFactory.CreateDbContext();
            var filesTask = db3.FilesOnFileSystem
                .AsNoTracking()
                .Where(f => f.CompanyId == companyId && !f.Deleted && f.UploadedBy == companyUser.Email)
                .Select(f => new { f.DirectAssign })
                .ToListAsync();

            await Task.WhenAll(companyTask, investigationsTask, filesTask);

            var company = await companyTask;

            var investigations = await investigationsTask;
            var files = await filesTask;

            // Compute counts in-memory
            int claimAssign = investigations.Count(c => c.InsuranceType == InsuranceType.CLAIM && assignableSubStatuses.Contains(c.SubStatus));
            int claimActive = investigations.Count(c => c.InsuranceType == InsuranceType.CLAIM && c.Status == CONSTANTS.CASE_STATUS.INPROGRESS && !activeExcludedSubStatuses.Contains(c.SubStatus));

            int underwritingAssign = investigations.Count(c => c.InsuranceType == InsuranceType.UNDERWRITING && assignableSubStatuses.Contains(c.SubStatus));
            int underwritingActive = investigations.Count(c => c.InsuranceType == InsuranceType.UNDERWRITING && c.Status == CONSTANTS.CASE_STATUS.INPROGRESS && !activeExcludedSubStatuses.Contains(c.SubStatus));

            int filesUpload = files.Count(f => !f.DirectAssign);
            int filesUploadAssign = files.Count(f => f.DirectAssign);

            // Build dashboard
            return new DashboardData
            {
                AutoAllocation = company.AutoAllocation,
                BulkUpload = company.BulkUpload,
                FirstBlockName = "ADD/ASSIGN",
                FirstBlockUrl = "/CaseCreateEdit/New",
                FirstBlockCount = claimAssign,
                SecondBlockCount = underwritingAssign,
                BulkUploadBlockName = "UPLOAD",
                BulkUploadBlockUrl = "/CaseUpload/Uploads",
                BulkUploadBlockCount = filesUpload,
                BulkUploadAssignCount = filesUploadAssign,
                ThirdBlockName = "ACTIVE",
                ThirdBlockCount = claimActive,
                LastBlockCount = underwritingActive,
                ThirdBlockUrl = "/CaseActive/Active"
            };
        }
    }
}