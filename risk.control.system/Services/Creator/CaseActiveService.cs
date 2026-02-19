using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Creator
{
    public interface ICaseActiveService
    {
        Task<CaseTransactionModel> GetActiveCaseDetails(string currentUserEmail, long id);

        Task<int> GetPendingUploadCount(string userEmail);
    }

    internal class CaseActiveService : ICaseActiveService
    {
        private readonly ApplicationDbContext _context;

        public CaseActiveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CaseTransactionModel> GetActiveCaseDetails(string userEmail, long id)
        {
            var caseTask = await _context.Investigations.AsNoTracking()
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.PolicyDetail)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.ClientCompany)
                .Include(c => c.Vendor)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (caseTask.CustomerDetail != null)
            {
                var maskedCustomerContact = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
                caseTask.CustomerDetail.PhoneNumber = maskedCustomerContact;
            }
            if (caseTask.BeneficiaryDetail != null)
            {
                var maskedBeneficiaryContact = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);
                caseTask.BeneficiaryDetail.PhoneNumber = maskedBeneficiaryContact;
            }
            var companyUser = await _context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();

            var timeTaken = DateTime.UtcNow - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";
            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Beneficiary = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                TimeTaken = totalTimeTaken,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR),
                Currency = CustomExtensions.GetCultureByCountry(companyUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
            };

            return model;
        }

        public async Task<int> GetPendingUploadCount(string userEmail)
        {
            var pendingCount = await _context.Investigations.CountAsync(c => c.UpdatedBy == userEmail && c.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_IN_PROGRESS);
            return pendingCount;
        }
    }
}