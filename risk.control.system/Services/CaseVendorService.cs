using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICaseVendorService
    {
        Task<CaseInvestigationVendorsModel> GetInvestigate(string userEmail, long selectedcase, bool uploaded = false);

        Task<CaseTransactionModel> GetInvestigatedForAgent(string currentUserEmail, long id);

        Task<CaseInvestigationVendorsModel> GetInvestigateReport(string userEmail, long selectedcase);

    }

    public class CaseVendorService : ICaseVendorService
    {
        private readonly ApplicationDbContext _context;

        public CaseVendorService(
            ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<CaseInvestigationVendorsModel> GetInvestigate(string userEmail, long selectedcase, bool uploaded = false)
        {
            var claim = await _context.Investigations
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
            if (claim is null)
            {
                return null;
            }
            var customerContactMasked = new string('*', claim.CustomerDetail.ContactNumber.ToString().Length - 4) + claim.CustomerDetail.ContactNumber.ToString().Substring(claim.CustomerDetail.ContactNumber.ToString().Length - 4);
            claim.CustomerDetail.ContactNumber = customerContactMasked;

            var beneficairyContactMasked = new string('*', claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4) + claim.BeneficiaryDetail.ContactNumber.ToString().Substring(claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4);

            claim.BeneficiaryDetail.ContactNumber = beneficairyContactMasked;

            claim.InvestigationReport.AgentEmail = userEmail;

            var templates = await _context.ReportTemplates
                .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.AgentIdReport)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationTemplate)
               .ThenInclude(l => l.MediaReports)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.Questions)
                   .FirstOrDefaultAsync(q => q.Id == claim.ReportTemplateId);

            claim.InvestigationReport.ReportTemplate = templates;
            _context.Investigations.Update(claim);
            var rows = await _context.SaveChangesAsync(null, false);

            var model = new CaseInvestigationVendorsModel
            {
                InvestigationReport = claim.InvestigationReport,
                Location = claim.BeneficiaryDetail,
                ClaimsInvestigation = claim
            };
            return model;
        }

        public async Task<CaseTransactionModel> GetInvestigatedForAgent(string currentUserEmail, long id)
        {
            var claim = await _context.Investigations
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
            if (claim is null) return null;

            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(u => u.Email == currentUserEmail);
            var lastHistory = claim.InvestigationTimeline.OrderByDescending(h => h.StatusChangedAt).FirstOrDefault();
            var endTIme = claim.Status == CONSTANTS.CASE_STATUS.FINISHED ? claim.ProcessedByAssessorTime.GetValueOrDefault() : DateTime.Now;
            var timeTaken = endTIme - claim.Created;
            var totalTimeTaken = timeTaken != TimeSpan.Zero
                ? $"{(timeTaken.Days > 0 ? $"{timeTaken.Days}d " : "")}" +
              $"{(timeTaken.Hours > 0 ? $"{timeTaken.Hours}h " : "")}" +
              $"{(timeTaken.Minutes > 0 ? $"{timeTaken.Minutes}m " : "")}" +
              $"{(timeTaken.Seconds > 0 ? $"{timeTaken.Seconds}s" : "less than a sec")}"
            : "-";

            var invoice = _context.VendorInvoice.FirstOrDefault(i => i.InvestigationReportId == claim.InvestigationReportId);
            var templates = await _context.ReportTemplates
               .Include(r => r.LocationTemplate)
                  .ThenInclude(l => l.AgentIdReport)
              .Include(r => r.LocationTemplate)
               .ThenInclude(l => l.MediaReports)
              .Include(r => r.LocationTemplate)
                  .ThenInclude(l => l.FaceIds)
              .Include(r => r.LocationTemplate)
                  .ThenInclude(l => l.DocumentIds)
              .Include(r => r.LocationTemplate)
                  .ThenInclude(l => l.Questions)
                  .FirstOrDefaultAsync(q => q.Id == claim.ReportTemplateId);

            claim.InvestigationReport.ReportTemplate = templates;

            var tracker = _context.PdfDownloadTracker
                          .FirstOrDefault(t => t.ReportId == id && t.UserEmail == currentUserEmail);
            bool canDownload = true;
            if (tracker != null)
            {
                canDownload = tracker.DownloadCount <= 3;
            }

            var model = new CaseTransactionModel
            {
                ClaimsInvestigation = claim,
                CaseIsValidToAssign = claim.IsValidCaseData(),
                Location = claim.BeneficiaryDetail,
                Assigned = claim.Status == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                AutoAllocation = companyUser != null ? companyUser.ClientCompany.AutoAllocation : false,
                TimeTaken = totalTimeTaken,
                VendorInvoice = invoice,
                CanDownload = canDownload,
                Withdrawable = (claim.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR)
            };

            return model;
        }
        public async Task<CaseInvestigationVendorsModel> GetInvestigateReport(string userEmail, long selectedcase)
        {
            var claim = await _context.Investigations
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
            if (claim is null) return null;

            var beneficiaryDetails = await _context.BeneficiaryDetail
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .FirstOrDefaultAsync(c => c.BeneficiaryDetailId == claim.BeneficiaryDetail.BeneficiaryDetailId);

            var customerContactMasked = new string('*', claim.CustomerDetail.ContactNumber.ToString().Length - 4) + claim.CustomerDetail.ContactNumber.ToString().Substring(claim.CustomerDetail.ContactNumber.ToString().Length - 4);
            claim.CustomerDetail.ContactNumber = customerContactMasked;

            var beneficairyContactMasked = new string('*', claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4) + claim.BeneficiaryDetail.ContactNumber.ToString().Substring(claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4);

            claim.BeneficiaryDetail.ContactNumber = beneficairyContactMasked;

            beneficiaryDetails.ContactNumber = beneficairyContactMasked;

            var caseReportTemplate = await _context.ReportTemplates
                .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.AgentIdReport)
               .Include(r => r.LocationTemplate)
               .ThenInclude(l => l.MediaReports)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.FaceIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.DocumentIds)
               .Include(r => r.LocationTemplate)
                   .ThenInclude(l => l.Questions)
                   .FirstOrDefaultAsync(q => q.Id == claim.ReportTemplateId);

            claim.InvestigationReport.ReportTemplate = caseReportTemplate;

            return (new CaseInvestigationVendorsModel
            {
                InvestigationReport = claim.InvestigationReport,
                Location = beneficiaryDetails,
                ClaimsInvestigation = claim,
                Address = claim.PolicyDetail.InsuranceType == Models.InsuranceType.CLAIM ? "Beneficiary" : "Life-Assured"
            });
        }
    }
}