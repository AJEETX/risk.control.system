using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface IWithdrawCaseService
    {
        Task<(string, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long caseId);
    }

    internal class WithdrawCaseService(ApplicationDbContext context, ILogger<WithdrawCaseService> logger, ITimelineService timelineService) : IWithdrawCaseService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<WithdrawCaseService> _logger = logger;
        private readonly ITimelineService _timelineService = timelineService;

        public async Task<(string, long)> WithdrawCaseByCompany(string userEmail, CaseTransactionModel model, long caseId)
        {
            try
            {
                var currentUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);
                var caseTask = await _context.Investigations
                    .Include(t => t.PolicyDetail)
                    .FirstOrDefaultAsync(c => c.Id == caseId);
                var vendorId = caseTask!.VendorId;
                var company = await _context.ClientCompany.FirstOrDefaultAsync(c => c.ClientCompanyId == caseTask.ClientCompanyId);

                caseTask.IsNew = true;
                caseTask.Updated = DateTime.UtcNow;
                caseTask.UpdatedBy = currentUser!.Email;
                caseTask.AssignedToAgency = false;
                caseTask.CaseOwner = company!.Email;
                caseTask.VendorId = null;
                caseTask.Vendor = null;
                caseTask.SubStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY;
                _context.Investigations.Update(caseTask);
                var rows = await _context.SaveChangesAsync(null, false) > 0;

                await _timelineService.UpdateTaskStatus(caseTask.Id, currentUser.Email!);

                return rows ? (caseTask.PolicyDetail!.ContractNumber!, vendorId.GetValueOrDefault()!) : (null!, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred withdraw case {Id}. {UserEmail}", caseId, userEmail);
                return (null!, 0);
            }
        }
    }
}