using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface IAgencyDashboardService
    {
        Task<DashboardData> GetSupervisorCount(string userEmail, string role);

        Task<DashboardData> GetAgentCount(string userEmail, string role);
    }

    internal class AgencyDashboardService : IAgencyDashboardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> contextFactory;
        private readonly ILogger<AgencyDashboardService> logger;

        // Consts remained the same for logic consistency
        private const string allocated = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;

        private const string assigned2Agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
        private const string submitted2Supervisor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;
        private const string submitted2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;
        private const string reply2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR;
        private const string requestedAssessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
        private const string rejectd = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
        private const string approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
        private const string finished = CONSTANTS.CASE_STATUS.FINISHED;

        public AgencyDashboardService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<AgencyDashboardService> logger)
        {
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        public async Task<DashboardData> GetAgentCount(string userEmail, string role)
        {
            try
            {
                await using var _context = contextFactory.CreateDbContext();
                var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == userEmail);

                // Execute the first count
                var taskCount = await GetCases(_context).CountAsync(c =>
                    c.VendorId == vendorUser.VendorId &&
                    c.SubStatus == assigned2Agent &&
                    c.TaskedAgentEmail == userEmail);

                // Execute the second count
                var agentSubmittedCount = await GetCases(_context).CountAsync(t =>
                    t.TaskedAgentEmail == userEmail &&
                    t.SubStatus != assigned2Agent);

                return new DashboardData
                {
                    FirstBlockName = "Tasks",
                    FirstBlockCount = taskCount,
                    FirstBlockUrl = "/Agent/Index",
                    SecondBlockName = "Submitted",
                    SecondBlockCount = agentSubmittedCount,
                    SecondBlockUrl = "/Agent/Submitted"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred for {UserEmail}", userEmail);
                throw;
            }
        }

        public async Task<DashboardData> GetSupervisorCount(string userEmail, string role)
        {
            try
            {
                // Execute queries in parallel
                var claimsAllocateTask = GetAgencyAllocateCount(userEmail);
                var claimsVerifiedTask = GetAgencyVerifiedCount(userEmail);
                var claimsActiveCountTask = GetSuperVisorActiveCount(userEmail);
                var claimsCompletedTask = GetAgencyyCompleted(userEmail);

                await Task.WhenAll(claimsAllocateTask, claimsVerifiedTask, claimsActiveCountTask, claimsCompletedTask);

                return new DashboardData
                {
                    FirstBlockName = "Allocate/Enquiry",
                    FirstBlockCount = await claimsAllocateTask,
                    FirstBlockUrl = "/VendorInvestigation/Allocate",
                    SecondBlockName = "Submit(report)",
                    SecondBlockCount = await claimsVerifiedTask,
                    SecondBlockUrl = "/VendorInvestigation/CaseReport",
                    ThirdBlockName = "Active",
                    ThirdBlockCount = await claimsActiveCountTask,
                    ThirdBlockUrl = "/VendorInvestigation/Open",
                    LastBlockName = "Completed",
                    LastBlockCount = await claimsCompletedTask,
                    LastBlockUrl = "/VendorInvestigation/Completed"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting supervisor dashboard data for user {UserEmail}", userEmail);
                throw;
            }
        }

        private async Task<int> GetSuperVisorActiveCount(string userEmail)
        {
            await using var _context = contextFactory.CreateDbContext();
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var query = GetAgencyClaims(_context).Where(a => a.VendorId == vendorUser.VendorId && a.Status != finished);

            if (vendorUser.IsVendorAdmin)
            {
                return await query.CountAsync(a => a.SubStatus == assigned2Agent ||
                                                   a.SubStatus == submitted2Assessor ||
                                                   a.SubStatus == reply2Assessor);
            }

            return await query.CountAsync(a => (a.SubStatus == assigned2Agent && a.AllocatingSupervisordEmail == userEmail) ||
                                               (a.SubStatus == submitted2Assessor && a.SubmittingSupervisordEmail == userEmail) ||
                                               (a.SubStatus == reply2Assessor && a.SubmittingSupervisordEmail == userEmail));
        }

        private async Task<int> GetAgencyVerifiedCount(string userEmail)
        {
            await using var _context = contextFactory.CreateDbContext();
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            return await GetAgencyClaims(_context).CountAsync(i => i.VendorId == vendorUser.VendorId && i.SubStatus == submitted2Supervisor);
        }

        private async Task<int> GetAgencyAllocateCount(string userEmail)
        {
            await using var _context = contextFactory.CreateDbContext();
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            return await GetAgencyClaims(_context).CountAsync(i => i.VendorId == vendorUser.VendorId &&
                (i.SubStatus == allocated || i.SubStatus == requestedAssessor));
        }

        private async Task<int> GetAgencyyCompleted(string userEmail)
        {
            await using var _context = contextFactory.CreateDbContext();
            var agencyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var query = GetAgencyClaims(_context).Where(c => c.VendorId == agencyUser.VendorId && c.Status == finished);

            if (agencyUser.IsVendorAdmin)
            {
                return await query.CountAsync(item => item.SubStatus == approved || item.SubStatus == rejectd);
            }

            return await query.CountAsync(item => item.SubmittingSupervisordEmail == userEmail &&
                (item.SubStatus == approved || item.SubStatus == rejectd));
        }

        private static IQueryable<InvestigationTask> GetCases(ApplicationDbContext context)
        {
            // Do NOT use a 'using' block here
            return context.Investigations
                .AsNoTracking()
                .Include(c => c.PolicyDetail)
                .Where(c => !c.Deleted);
        }

        private static IQueryable<InvestigationTask> GetAgencyClaims(ApplicationDbContext context)
        {
            return context.Investigations
                .AsNoTracking()
                .Where(c => !c.Deleted);
        }
    }
}