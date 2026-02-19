using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface ICaseReportService
    {
        Task<CaseAgencyModel> GetInvestigateReport(string userEmail, long selectedcase);
    }

    internal class CaseReportService : ICaseReportService
    {
        private readonly ApplicationDbContext _context;

        public CaseReportService(
            ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<CaseAgencyModel> GetInvestigateReport(string userEmail, long selectedcase)
        {
            var caseTask = await _context.Investigations.AsNoTracking()
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

            var beneficiaryDetails = await _context.BeneficiaryDetail.AsNoTracking()
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

            return (new CaseAgencyModel
            {
                InvestigationReport = caseTask.InvestigationReport,
                Beneficiary = beneficiaryDetails,
                CaseTask = caseTask,
                Address = caseTask.PolicyDetail.InsuranceType == Models.InsuranceType.CLAIM ? "Beneficiary" : "Life-Assured",
                Currency = CustomExtensions.GetCultureByCountry(caseTask.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
            });
        }
    }
}