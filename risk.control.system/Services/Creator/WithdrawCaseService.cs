using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IWithdrawCaseService
    {
        Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long caseId);
    }

    internal class WithdrawCaseService : IWithdrawCaseService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<WithdrawCaseService> logger;
        private readonly ITimelineService timelineService;

        public WithdrawCaseService(ApplicationDbContext context, ILogger<WithdrawCaseService> logger, ITimelineService timelineService)
        {
            this.context = context;
            this.logger = logger;
            this.timelineService = timelineService;
        }

        public async Task<(ClientCompany, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long caseId)
        {
            try
            {
                var currentUser = await context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await context.Investigations
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                var vendorId = caseTask.VendorId;
                var company = await context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                caseTask.IsNew = true;
                caseTask.Updated = DateTime.Now;
                caseTask.UpdatedBy = currentUser.Email;
                caseTask.AssignedToAgency = false;
                caseTask.CaseOwner = company.Email;
                caseTask.VendorId = null;
                caseTask.Vendor = null;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY;
                context.Investigations.Update(caseTask);
                var rows = await context.SaveChangesAsync(null, false) > 0;

                await timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email);

                return rows ? (company, vendorId.GetValueOrDefault()) : (null, 0);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred withdraw case {Id}. {UserEmail}", caseId, userEmail);
                return (null, 0);
            }
        }
    }
}