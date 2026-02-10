using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface IManagerDashboardService
    {
        Task<DashboardData> GetManagerCount(string userEmail, string role);
    }

    internal class ManagerDashboardService : IManagerDashboardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ManagerDashboardService> logger;

        public ManagerDashboardService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ManagerDashboardService> logger)
        {
            _contextFactory = contextFactory;
            this.logger = logger;
        }

        public async Task<DashboardData> GetManagerCount(string userEmail, string role)
        {
            try
            {
                var activeClaimsTask = GetManagerActive(userEmail, InsuranceType.CLAIM);
                var activeUnderwritingsTask = GetManagerActive(userEmail, InsuranceType.UNDERWRITING);

                var claimsCompletedTask = GetCompanyManagerApproved(userEmail, InsuranceType.CLAIM);
                var underwritingCompletedTask = GetCompanyManagerApproved(userEmail, InsuranceType.UNDERWRITING);

                var claimsRejectTask = GetManagerReject(userEmail, InsuranceType.CLAIM);
                var undewrwritingRejectTask = GetManagerReject(userEmail, InsuranceType.UNDERWRITING);

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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching manager dashboard data. {UserEmail}", userEmail);
                throw;
            }
        }

        private async Task<int> GetManagerActive(string userEmail, InsuranceType insuranceType)
        {
            await using var _context = _contextFactory.CreateDbContext();

            // Pass the context into GetCases
            var cases = GetCases(_context).Where(c => c.PolicyDetail.InsuranceType == insuranceType);

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
            await using var _context = _contextFactory.CreateDbContext();
            var cases = GetCases(_context).Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.Status == CONSTANTS.CASE_STATUS.FINISHED &&
                c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR
                );

            return count;
        }

        private async Task<int> GetManagerReject(string userEmail, InsuranceType insuranceType)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var cases = GetCases(_context).Where(c => c.PolicyDetail.InsuranceType == insuranceType);
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await cases.CountAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR && c.Status == CONSTANTS.CASE_STATUS.FINISHED);

            return count;
        }

        private async Task<int> GetEmpanelledAgencies(string userEmail)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var empAgencies = await _context.ClientCompany.Include(c => c.EmpanelledVendors).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);
            var count = empAgencies.EmpanelledVendors.Count(v => v.Status == VendorStatus.ACTIVE && !v.Deleted);
            return count;
        }

        private async Task<int> GetAvailableAgencies(string userEmail)
        {
            await using var _context = _contextFactory.CreateDbContext();

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
            var company = await _context.ClientCompany
               .Include(c => c.EmpanelledVendors)
               .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = await _context.Vendor
                .CountAsync(v => !company.EmpanelledVendors.Contains(v) && v.CountryId == companyUser.CountryId && !v.Deleted);
            return availableVendors;
        }

        // Accept the context as a parameter instead of creating it locally
        private IQueryable<InvestigationTask> GetCases(ApplicationDbContext context)
        {
            return context.Investigations
                .Include(c => c.PolicyDetail)
                .AsNoTracking();
        }
    }
}