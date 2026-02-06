using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface ICompanyDashboardService
    {
        Task<DashboardData> GetCreatorCount(string userEmail, string role);

        Task<DashboardData> GetCompanyAdminCount(string userEmail, string role);

        Task<DashboardData> GetAssessorCount(string userEmail, string role);

        Task<DashboardData> GetManagerCount(string userEmail, string role);
    }

    internal class CompanyDashboardService : ICompanyDashboardService
    {
        private const string submitted2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;
        private const string reply2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR;
        private const string requestedAssessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
        private const string rejectd = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
        private const string approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
        private const string finished = CONSTANTS.CASE_STATUS.FINISHED;
        private readonly ApplicationDbContext _context;

        public CompanyDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardData> GetCompanyAdminCount(string userEmail, string role)
        {
            var companyUsersCount = await GetCompanyUsers(userEmail);

            var data = new DashboardData();
            data.FirstBlockName = "All Users";
            data.FirstBlockCount = companyUsersCount;
            data.FirstBlockUrl = "/ManageCompanyUser/Users";

            return data;
        }

        public async Task<DashboardData> GetCreatorCount(string userEmail, string role)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var data = new DashboardData
            {
                AutoAllocation = company.AutoAllocation,
                BulkUpload = company.BulkUpload
            };

            var claimCountTask = GetCreatorAssignAuto(userEmail, InsuranceType.CLAIM);
            var underWritingCountTask = GetCreatorAssignAuto(userEmail, InsuranceType.UNDERWRITING);
            var filesUploadCountTask = _context.FilesOnFileSystem.CountAsync(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && !f.DirectAssign);
            var filesUploadAssignCountTask = _context.FilesOnFileSystem.CountAsync(f => f.CompanyId == company.ClientCompanyId && !f.Deleted && f.UploadedBy == companyUser.Email && f.DirectAssign);
            var claimsActiveTask = GetCreatorActive(userEmail, InsuranceType.CLAIM);
            var underWritingActiveTask = GetCreatorActive(userEmail, InsuranceType.UNDERWRITING);

            await Task.WhenAll(claimCountTask, underWritingCountTask, filesUploadCountTask, filesUploadAssignCountTask, claimsActiveTask, underWritingActiveTask);

            data.FirstBlockName = "ADD/ASSIGN";
            data.FirstBlockUrl = "/CaseCreateEdit/New";
            data.FirstBlockCount = await claimCountTask;
            data.SecondBlockCount = await underWritingCountTask;

            data.BulkUploadBlockName = "UPLOAD  ";

            data.BulkUploadBlockUrl = "/CaseUpload/Uploads";

            data.BulkUploadBlockCount = await filesUploadCountTask;
            data.BulkUploadAssignCount = await filesUploadAssignCountTask;

            data.ThirdBlockName = "ACTIVE";

            data.ThirdBlockCount = await claimsActiveTask;
            data.LastBlockCount = await underWritingActiveTask;
            data.ThirdBlockUrl = "/CaseActive/Active";

            return data;
        }

        public async Task<DashboardData> GetAssessorCount(string userEmail, string role)
        {
            var claimsAssessorTask = GetAssessorAssess(userEmail, InsuranceType.CLAIM);
            var underwritingAssessorTask = GetAssessorAssess(userEmail, InsuranceType.UNDERWRITING);

            var claimsReviewTask = GetAssessorReview(userEmail, InsuranceType.CLAIM);
            var underwritingReviewTask = GetAssessorReview(userEmail, InsuranceType.UNDERWRITING);

            var claimsRejectTask = GetAssessorReject(userEmail, InsuranceType.CLAIM);
            var underwritingRejectTask = GetAssessorReject(userEmail, InsuranceType.UNDERWRITING);

            var claimsCompletedTask = GetCompanyCompleted(userEmail, InsuranceType.CLAIM);
            var underwritingCompletedTask = GetCompanyCompleted(userEmail, InsuranceType.UNDERWRITING);

            await Task.WhenAll(claimsAssessorTask, underwritingAssessorTask, claimsReviewTask, underwritingReviewTask, claimsRejectTask, underwritingRejectTask, claimsCompletedTask, underwritingCompletedTask);
            var data = new DashboardData();
            data.FirstBlockName = "Assess (report)";
            data.FirstBlockCount = await claimsAssessorTask;
            data.UnderwritingCount = await underwritingAssessorTask;
            data.FirstBlockUrl = "/Assessor/Assessor";

            data.SecondBlockName = "Enquiry";
            data.SecondBlockCount = await claimsReviewTask;
            data.SecondBBlockCount = await underwritingReviewTask;
            data.SecondBlockUrl = "/Assessor/Review";

            data.ThirdBlockName = "Approved";
            data.ApprovedClaimgCount = await claimsCompletedTask;
            data.ApprovedUnderwritingCount = await underwritingCompletedTask;
            data.ThirdBlockUrl = "/Assessor/Approved";

            data.LastBlockName = "Rejected";
            data.RejectedClaimCount = await claimsRejectTask;
            data.RejectedUnderwritingCount = await underwritingRejectTask;
            data.LastBlockUrl = "/Assessor/Rejected";

            return data;
        }

        public async Task<DashboardData> GetManagerCount(string userEmail, string role)
        {
            var claimsRejectTask = GetManagerReject(userEmail, InsuranceType.CLAIM);

            var undewrwritingRejectTask = GetManagerReject(userEmail, InsuranceType.UNDERWRITING);

            var claimsCompletedTask = GetCompanyManagerApproved(userEmail, InsuranceType.CLAIM);
            var underwritingCompletedTask = GetCompanyManagerApproved(userEmail, InsuranceType.UNDERWRITING);

            var activeClaimsTask = GetManagerActive(userEmail, InsuranceType.CLAIM);
            var activeUnderwritingsTask = GetManagerActive(userEmail, InsuranceType.UNDERWRITING);

            var empanelledAgenciesCountTask = GetEmpanelledAgencies(userEmail);
            var availableAgenciesCountTask = GetAvailableAgencies(userEmail);

            await Task.WhenAll(claimsRejectTask, undewrwritingRejectTask, claimsCompletedTask, underwritingCompletedTask, activeClaimsTask, activeUnderwritingsTask, empanelledAgenciesCountTask, availableAgenciesCountTask);

            var data = new DashboardData();

            data.SecondBBlockName = "Active";
            data.SecondBBlockUrl = "/Manager/Active";
            data.SecondBBlockCount = await activeClaimsTask;
            data.SecondBlockCount = await activeUnderwritingsTask;

            data.ThirdBlockName = "Approved";
            data.ThirdBlockCount = await claimsCompletedTask;
            data.ApprovedUnderwritingCount = await underwritingCompletedTask;
            data.ThirdBlockUrl = "/Manager/Approved";

            data.LastBlockName = "Rejected";
            data.LastBlockCount = await claimsRejectTask;
            data.RejectedUnderwritingCount = await undewrwritingRejectTask;
            data.LastBlockUrl = "/Manager/Rejected";

            data.FifthBlockName = "Empanelled Agencies";
            data.FifthBlockCount = await empanelledAgenciesCountTask;
            data.FifthBlockUrl = "/EmpanelledAgency/Agencies";

            data.SixthBlockName = "Available Agencies";
            data.SixthBlockCount = await availableAgenciesCountTask;
            data.SixthBlockUrl = "/AvailableAgency/Agencies";

            return data;
        }

        private async Task<int> GetAvailableAgencies(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var company = await _context.ClientCompany
               .Include(c => c.EmpanelledVendors)
               .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = await _context.Vendor
                .CountAsync(v => !company.EmpanelledVendors.Contains(v) && v.CountryId == companyUser.CountryId && !v.Deleted);
            return availableVendors;
        }

        private async Task<int> GetEmpanelledAgencies(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var empAgencies = await _context.ClientCompany.Include(c => c.EmpanelledVendors).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var count = empAgencies.EmpanelledVendors.Count(v => v.Status == VendorStatus.ACTIVE && !v.Deleted);
            return count;
        }

        private async Task<int> GetManagerActive(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
                    a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                    (a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                    a.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR));

            return count;
        }

        private async Task<int> GetCompanyManagerApproved(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.Status == finished &&
                c.SubStatus == approved
                );

            return count;
        }

        private async Task<int> GetManagerReject(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == rejectd && c.Status == finished);

            return count;
        }

        private async Task<int> GetCompanyCompleted(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubmittedAssessordEmail == userEmail &&
                c.Status == finished &&
                (c.SubStatus == approved)
                );

            return count;
        }

        private async Task<int> GetAssessorReject(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == rejectd && c.Status == finished && c.SubmittedAssessordEmail == userEmail
                );

            return count;
        }

        private async Task<int> GetAssessorReview(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var count = await cases.CountAsync(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
            a.SubStatus == requestedAssessor && a.RequestedAssessordEmail == userEmail);
            return count;
        }

        private async Task<int> GetAssessorAssess(string userEmail, InsuranceType insuranceType)
        {
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
            i.SubStatus == submitted2Assessor ||
            i.SubStatus == reply2Assessor
             );

            return count;
        }

        private async Task<int> GetCompanyUsers(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);

            var allCompanyUserCount = await _context.ApplicationUser.CountAsync(u => u.ClientCompanyId == companyUser.ClientCompanyId && !u.Deleted && u.Email != userEmail);

            return allCompanyUserCount;
        }

        private async Task<int> GetCreatorActive(string userEmail, InsuranceType insuranceType)
        {
            var subStatus = new[]
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
            var cases = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(a => !a.Deleted && a.Status == CONSTANTS.CASE_STATUS.INPROGRESS &&
                            a.ClientCompanyId == companyUser.ClientCompanyId && a.CreatedUser == userEmail &&
                            !subStatus
                            .Contains(a.SubStatus));

            return count;
        }

        private async Task<int> GetCreatorAssignAuto(string userEmail, InsuranceType insuranceType)
        {
            var claims = GetCases().Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await claims.CountAsync(a => !a.Deleted &&
                a.ClientCompanyId == companyUser.ClientCompanyId &&
                     a.CreatedUser == companyUser.Email &&
                         (
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR ||
                         a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY ||
                        a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
                    ));
            return count;
        }

        private IQueryable<InvestigationTask> GetCases()
        {
            return _context.Investigations
                .Include(c => c.PolicyDetail)
                .AsNoTracking();
        }
    }
}