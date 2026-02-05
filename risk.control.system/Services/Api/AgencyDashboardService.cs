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
        private readonly ApplicationDbContext _context;

        private const string allocated = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
        private const string assigned2Agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
        private const string submitted2Supervisor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR;
        private const string submitted2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR;
        private const string reply2Assessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR;
        private const string requestedAssessor = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
        private const string rejectd = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;
        private const string approved = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
        private const string finished = CONSTANTS.CASE_STATUS.FINISHED;

        public AgencyDashboardService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<DashboardData> GetAgentCount(string userEmail, string role)
        {
            var vendorUser = await _context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);
            var taskCountTask = GetCases().CountAsync(c => c.VendorId == vendorUser.VendorId &&
            c.SubStatus == assigned2Agent &&
            c.TaskedAgentEmail == userEmail);

            var agentSubmittedCountTask = GetCases().Distinct().CountAsync(t => t.TaskedAgentEmail == userEmail && t.SubStatus != assigned2Agent);

            await Task.WhenAll(taskCountTask, agentSubmittedCountTask);

            var data = new DashboardData();
            data.FirstBlockName = "Tasks";
            data.FirstBlockCount = await taskCountTask;
            data.FirstBlockUrl = "/Agent/Index";

            data.SecondBlockName = "Submitted";
            data.SecondBlockCount = await agentSubmittedCountTask;
            data.SecondBlockUrl = "/Agent/Submitted";

            return data;
        }

        public async Task<DashboardData> GetSupervisorCount(string userEmail, string role)
        {
            var claimsAllocateTask = GetAgencyAllocateCount(userEmail);
            var claimsVerifiedTask = GetAgencyVerifiedCount(userEmail);
            var claimsActiveCountTask = GetSuperVisorActiveCount(userEmail);
            var claimsCompletedTask = GetAgencyyCompleted(userEmail);

            await Task.WhenAll(claimsAllocateTask, claimsVerifiedTask, claimsActiveCountTask, claimsCompletedTask);

            var data = new DashboardData();
            data.FirstBlockName = "Allocate/Enquiry";
            data.FirstBlockCount = await claimsAllocateTask;
            data.FirstBlockUrl = "/VendorInvestigation/Allocate";

            data.SecondBlockName = "Submit(report)";
            data.SecondBlockCount = await claimsVerifiedTask;
            data.SecondBlockUrl = "/VendorInvestigation/CaseReport";

            data.ThirdBlockName = "Active";
            data.ThirdBlockCount = await claimsActiveCountTask;
            data.ThirdBlockUrl = "/VendorInvestigation/Open";

            data.LastBlockName = "Completed";
            data.LastBlockCount = await claimsCompletedTask;
            data.LastBlockUrl = "/VendorInvestigation/Completed";

            return data;
        }

        private async Task<int> GetSuperVisorActiveCount(string userEmail)
        {
            var claims = GetAgencyClaims();
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            if (vendorUser.IsVendorAdmin)
            {
                return await claims.CountAsync(a => a.VendorId == vendorUser.VendorId &&
            a.Status != finished &&
                 (a.SubStatus == assigned2Agent ||
                a.SubStatus == submitted2Assessor ||
                 a.SubStatus == reply2Assessor));
            }
            var count = await claims.CountAsync(a => a.VendorId == vendorUser.VendorId &&
            a.Status != finished &&
                 ((a.SubStatus == assigned2Agent && a.AllocatingSupervisordEmail == userEmail) ||
                (a.SubStatus == submitted2Assessor && a.SubmittingSupervisordEmail == userEmail) ||
                 (a.SubStatus == reply2Assessor && a.SubmittingSupervisordEmail == userEmail)));
            return count;
        }

        private async Task<int> GetAgencyVerifiedCount(string userEmail)
        {
            var agencyCases = GetAgencyClaims();

            var vendorUser = await _context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(c => c.Email == userEmail);

            var count = await agencyCases.CountAsync(i => i.VendorId == vendorUser.VendorId &&
            i.SubStatus == submitted2Supervisor);
            return count;
        }

        private async Task<int> GetAgencyAllocateCount(string userEmail)
        {
            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var agencyCases = GetAgencyClaims().Where(i => i.VendorId == vendorUser.VendorId);

            var count = await agencyCases
                    .CountAsync(i => i.SubStatus == allocated ||
                    i.SubStatus == requestedAssessor);

            return count;
        }

        private async Task<int> GetAgencyyCompleted(string userEmail)
        {
            var agencyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var applicationDbContext = GetAgencyClaims().Where(c =>
                c.CustomerDetail != null && c.VendorId == agencyUser.VendorId);
            if (agencyUser.IsVendorAdmin)
            {
                var claimsSubmitted = 0;
                foreach (var item in applicationDbContext)
                {
                    if (item.Status == finished &&
                        item.SubStatus == approved ||
                        item.SubStatus == rejectd
                        )
                    {
                        claimsSubmitted += 1;
                    }
                }
                return claimsSubmitted;
            }
            else
            {
                var claimsSubmitted = 0;
                foreach (var item in applicationDbContext)
                {
                    if (item.SubmittingSupervisordEmail == userEmail && item.Status == finished &&
                        (item.SubStatus == approved ||
                        item.SubStatus == rejectd)
                        )
                    {
                        claimsSubmitted += 1;
                    }
                }
                return claimsSubmitted;
            }
        }

        private IQueryable<InvestigationTask> GetCases()
        {
            var applicationDbContext = _context.Investigations
               .Include(c => c.PolicyDetail)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }

        private IQueryable<InvestigationTask> GetAgencyClaims()
        {
            var applicationDbContext = _context.Investigations
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderBy(o => o.Created);
        }
    }
}