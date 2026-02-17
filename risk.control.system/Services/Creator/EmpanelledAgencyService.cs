using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Creator
{
    public interface IEmpanelledAgencyService
    {
        Task<CaseInvestigationVendorsModel> GetEmpanelledVendors(long selectedcase, string userEmail, long vendorId, bool fromEditPage = false);

        Task<ReportTemplate> GetReportTemplate(long caseId);
    }

    internal class EmpanelledAgencyService : IEmpanelledAgencyService
    {
        private readonly ApplicationDbContext _context;

        public EmpanelledAgencyService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task<CaseInvestigationVendorsModel> GetEmpanelledVendors(long selectedcase, string userEmail, long vendorId, bool fromEditPage = false)
        {
            var caseTask = await _context.Investigations
                .Include(c => c.CaseNotes)
                .Include(c => c.PolicyDetail)
                .Include(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CostCentre)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.Country)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.InvestigationServiceType)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.Id == selectedcase);

            var beneficiary = await _context.BeneficiaryDetail
               .Include(c => c.PinCode)
               .Include(c => c.BeneficiaryRelation)
               .Include(c => c.District)
               .Include(c => c.State)
               .Include(c => c.Country)
               .FirstOrDefaultAsync(c => c.InvestigationTaskId == selectedcase);

            var currentUser = await _context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

            _context.Investigations.Update(caseTask);
            await _context.SaveChangesAsync();

            return new CaseInvestigationVendorsModel
            {
                Beneficiary = beneficiary,
                Currency = CustomExtensions.GetCultureByCountry(currentUser.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol,
                VendorId = vendorId,
                FromEditPage = fromEditPage,
                CaseTask = caseTask
            };
        }

        public async Task<ReportTemplate> GetReportTemplate(long caseId)
        {
            var claimsInvestigation = await _context.Investigations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == caseId);

            var template = await _context.ReportTemplates
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
                .FirstOrDefaultAsync(r => r.Id == claimsInvestigation.ReportTemplateId);

            return template;
        }
    }
}