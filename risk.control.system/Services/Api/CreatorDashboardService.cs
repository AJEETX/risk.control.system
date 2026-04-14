using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface ICreatorDashboardService
    {
        Task<DashboardData> GetCreatorCount(string userEmail);
    }

    internal class CreatorDashboardService(IDbContextFactory<ApplicationDbContext> contextFactory) : ICreatorDashboardService
    {
        private static string[] assignableSubStatuses =
            [
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
            ];
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

        public async Task<DashboardData> GetCreatorCount(string userEmail)
        {
            await using var db1 = _contextFactory.CreateDbContext();
            var companyUser = await db1.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
            if (companyUser == null || !companyUser.ClientCompanyId.HasValue)
                throw new InvalidOperationException($"User '{userEmail}' not found or missing ClientCompanyId.");

            var companyId = companyUser.ClientCompanyId.Value;
            var companyTask = db1.ClientCompany.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCompanyId == companyId);
            await using var db2 = _contextFactory.CreateDbContext();
            var investigationsTask = db2.Investigations.AsNoTracking().Where(c => c.ClientCompanyId == companyId && c.CreatedUser == userEmail && !c.Deleted)
                .Select(c => new
                {
                    InsuranceType = c.PolicyDetail != null ? c.PolicyDetail.InsuranceType : (InsuranceType?)null,
                    SubStatus = c.SubStatus,
                    Status = c.Status
                }).ToListAsync();

            await using var db3 = _contextFactory.CreateDbContext();
            var filesTask = db3.FilesOnFileSystem.AsNoTracking().Where(f => f.CompanyId == companyId && !f.Deleted && f.UploadedBy == companyUser.Email)
                .Select(f => new { f.DirectAssign }).ToListAsync();
            await Task.WhenAll(companyTask, investigationsTask, filesTask);
            var company = await companyTask;
            var investigations = await investigationsTask;
            var files = await filesTask;

            int claimAssign = investigations.Count(c => c.InsuranceType == InsuranceType.CLAIM && assignableSubStatuses.Contains(c.SubStatus));
            int claimActive = investigations.Count(c => c.InsuranceType == InsuranceType.CLAIM && c.Status == CONSTANTS.CASE_STATUS.INPROGRESS && !CONSTANTS.ActiveSubStatuses.Contains(c.SubStatus));
            int underwritingAssign = investigations.Count(c => c.InsuranceType == InsuranceType.UNDERWRITING && assignableSubStatuses.Contains(c.SubStatus));
            int underwritingActive = investigations.Count(c => c.InsuranceType == InsuranceType.UNDERWRITING && c.Status == CONSTANTS.CASE_STATUS.INPROGRESS && !CONSTANTS.ActiveSubStatuses.Contains(c.SubStatus));
            int filesUpload = files.Count(f => !f.DirectAssign);
            int filesUploadAssign = files.Count(f => f.DirectAssign);

            return new DashboardData
            {
                FirstBlockName = "ADD/ASSIGN",
                FirstBlockUrl = "/AddAssign/New",
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