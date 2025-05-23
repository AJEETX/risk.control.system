using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICaseVendorService
    {
        Task<CaseInvestigationVendorsModel> GetInvestigate(string userEmail, long selectedcase, bool uploaded = false);

        Task<CaseInvestigationVendorsModel> GetInvestigateReport(string userEmail, long selectedcase);

    }

    public class CaseVendorService : ICaseVendorService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly ApplicationDbContext _context;
        private readonly IDashboardService dashboardService;
        private readonly IInvestigationService investigationService;
        private readonly IFeatureManager featureManager;
        private readonly IClaimsService claimsService;

        public CaseVendorService(
            UserManager<VendorApplicationUser> userManager,
            ApplicationDbContext context,
            IDashboardService dashboardService,
            IInvestigationService investigationService,
            IFeatureManager featureManager,
            IClaimsService claimsService)
        {
            this.userManager = userManager;
            this._context = context;
            this.dashboardService = dashboardService;
            this.investigationService = investigationService;
            this.featureManager = featureManager;
            this.claimsService = claimsService;
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
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.CaseQuestionnaire)
                .ThenInclude(c => c.Questions)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.PanIdReport)
                 .Include(c => c.InvestigationReport)
                .ThenInclude(c => c.AgentIdReport)
                .FirstOrDefaultAsync(c => c.Id == selectedcase);

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
            var rows = await _context.SaveChangesAsync();

            var model = new CaseInvestigationVendorsModel
            {
                InvestigationReport = claim.InvestigationReport,
                Location = claim.BeneficiaryDetail,
                ClaimsInvestigation = claim
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

            var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;
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

            return (new CaseInvestigationVendorsModel
            {
                InvestigationReport = claim.InvestigationReport,
                Location = beneficiaryDetails,
                ClaimsInvestigation = claim
            });
        }
    }
}