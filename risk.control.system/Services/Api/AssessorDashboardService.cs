using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface IAssessorDashboardService
    {
        Task<DashboardData> GetAssessorCount(string userEmail, string role);
    }

    internal class AssessorDashboardService : IAssessorDashboardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public AssessorDashboardService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
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

        private async Task<int> GetAssessorAssess(string userEmail, InsuranceType insuranceType)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var cases = GetCases(_context).Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(i => i.ClientCompanyId == companyUser.ClientCompanyId &&
            i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ||
            i.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR
             );

            return count;
        }

        private async Task<int> GetAssessorReview(string userEmail, InsuranceType insuranceType)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var cases = GetCases(_context).Where(c => c.PolicyDetail.InsuranceType == insuranceType);

            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);
            var count = await cases.CountAsync(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR && a.RequestedAssessordEmail == userEmail);
            return count;
        }

        private async Task<int> GetAssessorReject(string userEmail, InsuranceType insuranceType)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var cases = GetCases(_context).Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR &&
                c.Status == CONSTANTS.CASE_STATUS.FINISHED && c.SubmittedAssessordEmail == userEmail
                );

            return count;
        }

        private async Task<int> GetCompanyCompleted(string userEmail, InsuranceType insuranceType)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var cases = GetCases(_context).Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubmittedAssessordEmail == userEmail &&
                c.Status == CONSTANTS.CASE_STATUS.FINISHED &&
                (c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR)
                );

            return count;
        }

        private IQueryable<InvestigationTask> GetCases(ApplicationDbContext context)
        {
            return context.Investigations
                .Include(c => c.PolicyDetail)
                .Where(c => !c.Deleted)
                .AsNoTracking();
        }
    }
}