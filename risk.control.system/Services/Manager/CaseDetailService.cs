using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Manager
{
    public interface ICaseDetailService
    {
        Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id);
    }

    internal class CaseDetailService : ICaseDetailService
    {
        private readonly ApplicationDbContext _context;

        public CaseDetailService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<CaseTransactionModel> GetClaimDetailsReport(string currentUserEmail, long id)
        {
            var caseTask = await _context.Investigations.AsNoTracking()
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
                .Include(c => c.InvestigationTimeline)
                .Include(c => c.InvestigationReport.EnquiryRequest)
                .Include(c => c.InvestigationReport.EnquiryRequests)
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
            var companyUser = await _context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = caseTask.Status == CONSTANTS.CASE_STATUS.FINISHED ? caseTask.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.UtcNow;
            var timeTaken = endTIme - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await _context.VendorInvoice.AsNoTracking().FirstOrDefaultAsync(i => i.InvestigationReportId == caseTask.InvestigationReportId);
            var templates = await _context.ReportTemplates.AsNoTracking()
               .Include(r => r.LocationReport)
                  .ThenInclude(l => l.AgentIdReport)
              .Include(r => r.LocationReport)
               .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationReport)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == caseTask.ReportTemplateId);

            caseTask.InvestigationReport.ReportTemplate = templates;

            var tracker = await _context.PdfDownloadTracker.AsNoTracking()
                          .FirstOrDefaultAsync(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Beneficiary = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR),
                Currency = CustomExtensions.GetCultureByCountry(companyUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
            };

            return model;
        }
    }
}