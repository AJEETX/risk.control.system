using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Agency
{
    public interface IDeclineCaseService
    {
        Task<Vendor> DeclineCaseByAgency(string userEmail, CaseTransactionModel model, long caseId);

        Task<Vendor> WithdrawCaseFromAgent(string userEmail, CaseTransactionModel model, long caseId);
    }

    internal class DeclineCaseService : IDeclineCaseService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<DeclineCaseService> logger;
        private readonly ITimelineService timelineService;

        public DeclineCaseService(ApplicationDbContext context, ILogger<DeclineCaseService> logger, ITimelineService timelineService)
        {
            this.context = context;
            this.logger = logger;
            this.timelineService = timelineService;
        }

        public async Task<Vendor> WithdrawCaseFromAgent(string userEmail, CaseTransactionModel model, long caseId)
        {
            try
            {
                var currentUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await context.Investigations
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                caseTask.IsNewAssignedToAgency = true;
                caseTask.CaseOwner = currentUser.Vendor.Email;
                caseTask.TaskedAgentEmail = null;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;
                context.Investigations.Update(caseTask);
                var rows = await context.SaveChangesAsync(null, false);

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);

                return currentUser.Vendor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred decline case {Id}. {UserEmail}", caseId, userEmail);
                return null!;
            }
        }

        public async Task<Vendor> DeclineCaseByAgency(string userEmail, CaseTransactionModel model, long caseId)
        {
            try
            {
                var currentUser = await context.ApplicationUser.Include(u => u.Vendor).FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await context.Investigations
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);
                caseTask.CaseOwner = company.Email;
                caseTask.IsAutoAllocated = false;
                caseTask.IsNew = true;
                caseTask.IsNewAssignedToAgency = true;
                caseTask.IsNewSubmittedToAgent = true;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.AssignedToAgency = false;
                caseTask.VendorId = null;
                caseTask.Vendor = null;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY;
                context.Investigations.Update(caseTask);
                var rows = await context.SaveChangesAsync(null, false);
                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);
                return currentUser.Vendor;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred withdraw case {Id}. {UserEmail}", caseId, userEmail);
                return null!;
            }
        }
    }
}