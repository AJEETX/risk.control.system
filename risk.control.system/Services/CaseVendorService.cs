using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICaseVendorService
    {
        Task<CaseInvestigationVendorsModel> GetInvestigate(string userEmail, long selectedcase, bool uploaded = false);

        Task<CaseTransactionModel> GetInvestigatedForAgent(string currentUserEmail, long id);

        Task<CaseInvestigationVendorsModel> GetInvestigateReport(string userEmail, long selectedcase);

    }

    internal class CaseVendorService : ICaseVendorService
    {
        private readonly ApplicationDbContext _context;

        public CaseVendorService(
            ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<CaseInvestigationVendorsModel> GetInvestigate(string userEmail, long selectedcase, bool uploaded = false)
        {
            var caseTask = await _context.Investigations
                .Include(c => c.ClientCompany)
                .ThenInclude(c => c.Country)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
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
                .Include(c => c.CaseNotes)
                .Include(t => t.InvestigationReport)
                .FirstOrDefaultAsync(c => c.Id == selectedcase);
            if (caseTask is null)
            {
                return null;
            }
            var customerContactMasked = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseTask.CustomerDetail.PhoneNumber = customerContactMasked;

            var beneficairyContactMasked = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);

            caseTask.BeneficiaryDetail.PhoneNumber = beneficairyContactMasked;

            caseTask.InvestigationReport.AgentEmail = userEmail;

            var templates = await _context.ReportTemplates
                .Include(r => r.LocationReport)
                   .ThenInclude(l => l.AgentIdReport)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationReport)
               .ThenInclude(l => l.MediaReports)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationReport)
                   .ThenInclude(l => l.Questions)
                   .FirstOrDefaultAsync(q => q.Id == caseTask.ReportTemplateId);

            caseTask.InvestigationReport.ReportTemplate = templates;
            _context.Investigations.Update(caseTask);
            var rows = await _context.SaveChangesAsync(null, false);

            var model = new CaseInvestigationVendorsModel
            {
                InvestigationReport = caseTask.InvestigationReport,
                Location = caseTask.BeneficiaryDetail,
                ClaimsInvestigation = caseTask
            };
            return model;
        }

        public async Task<CaseTransactionModel> GetInvestigatedForAgent(string currentUserEmail, long id)
        {
            var caseTask = await _context.Investigations
                .Include(c => c.CaseMessages)
                .Include(c => c.CaseNotes)
                .Include(c => c.InvestigationReport)
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
            if (caseTask is null) return null;

            var companyUser = await _context.ApplicationUser.Include(u => u.ClientCompany).FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var lastHistory = caseTask.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = caseTask.Status == CONSTANTS.CASE_STATUS.FINISHED ? caseTask.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.Now;
            var timeTaken = endTIme - caseTask.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = await _context.VendorInvoice.FirstOrDefaultAsync(i => i.InvestigationReportId == caseTask.InvestigationReportId);
            var templates = await _context.ReportTemplates
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

            var tracker = _context.PdfDownloadTracker
                          .FirstOrDefault(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = caseTask,
                CaseIsValidToAssign = caseTask.IsValidCaseData(),
                Location = caseTask.BeneficiaryDetail,
                Assigned = caseTask.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (caseTask.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseInvestigationVendorsModel> GetInvestigateReport(string userEmail, long selectedcase)
        {
            var caseTask = await _context.Investigations
               .Include(c => c.InvestigationTimeline)
               .Include(c => c.InvestigationReport)
               .ThenInclude(c => c.EnquiryRequest)
               .Include(c => c.InvestigationReport)
               .ThenInclude(c => c.EnquiryRequests)
               .Include(c => c.Vendor)
               .Include(c => c.ClientCompany)
               .ThenInclude(c => c.Country)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.InvestigationServiceType)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.District)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.State)
               .Include(c => c.CustomerDetail)
               .ThenInclude(c => c.Country)
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
               .Include(c => c.CaseNotes)
               .Include(c => c.CaseMessages)
               .FirstOrDefaultAsync(c => c.Id == selectedcase);
            if (caseTask is null) return null;

            var beneficiaryDetails = await _context.BeneficiaryDetail
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .FirstOrDefaultAsync(c => c.BeneficiaryDetailId == caseTask.BeneficiaryDetail.BeneficiaryDetailId);

            var customerContactMasked = new string('*', caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4) + caseTask.CustomerDetail.PhoneNumber.ToString().Substring(caseTask.CustomerDetail.PhoneNumber.ToString().Length - 4);
            caseTask.CustomerDetail.PhoneNumber = customerContactMasked;

            var beneficairyContactMasked = new string('*', caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4) + caseTask.BeneficiaryDetail.PhoneNumber.ToString().Substring(caseTask.BeneficiaryDetail.PhoneNumber.ToString().Length - 4);

            caseTask.BeneficiaryDetail.PhoneNumber = beneficairyContactMasked;

            beneficiaryDetails.PhoneNumber = beneficairyContactMasked;

            var caseReportTemplate = await _context.ReportTemplates
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

            caseTask.InvestigationReport.ReportTemplate = caseReportTemplate;

            return (new CaseInvestigationVendorsModel
            {
                InvestigationReport = caseTask.InvestigationReport,
                Location = beneficiaryDetails,
                ClaimsInvestigation = caseTask,
                Address = caseTask.PolicyDetail.InsuranceType == Models.InsuranceType.CLAIM ? "Beneficiary" : "Life-Assured"
            });
        }
    }
}