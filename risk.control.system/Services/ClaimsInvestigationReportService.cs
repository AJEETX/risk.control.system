using risk.control.system.Models.ViewModel;
using risk.control.system.Data;
using risk.control.system.AppConstant;

using risk.control.system.Data;

using risk.control.system.Models;

using risk.control.system.Models.ViewModel;

using risk.control.system.Services;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;

namespace risk.control.system.Services
{
    public interface IClaimsInvestigationReportService
    {
        Task<ClaimsInvestigationVendorsModel> GetInvestigateReport(string currentUserEmail, string selectedcase);

        Task<ClaimTransactionModel> GetApprovedReport(string selectedcase);
    }

    public class ClaimsInvestigationReportService : IClaimsInvestigationReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientService httpClientService;

        public ClaimsInvestigationReportService(ApplicationDbContext context, IHttpClientService httpClientService)
        {
            this._context = context;
            this.httpClientService = httpClientService;
        }

        public async Task<ClaimTransactionModel> GetApprovedReport(string selectedcase)
        {
            var caseLogs = await _context.InvestigationTransaction
                .Include(i => i.InvestigationCaseStatus)
                .Include(i => i.InvestigationCaseSubStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.CaseLocations)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseStatus)
                .Include(c => c.ClaimsInvestigation)
                .ThenInclude(i => i.InvestigationCaseSubStatus)
                .Where(t => t.ClaimsInvestigationId == selectedcase)
                .OrderByDescending(c => c.HopCount)?.ToListAsync();

            var claimsInvestigation = await _context.ClaimsInvestigation
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.ClientCompany)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CaseEnabler)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.CostCentre)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.InvestigationCaseSubStatus)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CaseLocations)
              .ThenInclude(c => c.BeneficiaryRelation)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.Country)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.District)
              .Include(c => c.InvestigationCaseStatus)
              .Include(c => c.InvestigationCaseSubStatus)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.InvestigationServiceType)
              .Include(c => c.PolicyDetail)
              .ThenInclude(c => c.LineOfBusiness)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.PinCode)
              .Include(c => c.CustomerDetail)
              .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

            var location = await _context.CaseLocation
                .Include(l => l.ClaimReport)
                .Include(l => l.Vendor)
                .FirstOrDefaultAsync(l => l.ClaimsInvestigationId == selectedcase);

            var model = new ClaimTransactionModel
            {
                Claim = claimsInvestigation,
                Log = caseLogs,
                Location = location
            };


            var serviceCost = location.Vendor;
            var vendor = _context.Vendor.Include(v => v.VendorInvestigationServiceTypes).FirstOrDefault(v => v.VendorId == location.VendorId);

            var investigationServiced = vendor.VendorInvestigationServiceTypes.FirstOrDefault(s => s.InvestigationServiceTypeId == claimsInvestigation.PolicyDetail.InvestigationServiceTypeId);
            if (investigationServiced != null)
            {
                model.Price = investigationServiced.Price;
            }
            return model;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetInvestigateReport(string currentUserEmail, string selectedcase)
        {
            var claimsInvestigation = _context.ClaimsInvestigation
                 .Include(c => c.PolicyDetail)
                 .ThenInclude(c => c.ClientCompany)
                 .Include(c => c.PolicyDetail)
                 .ThenInclude(c => c.CaseEnabler)
                 .Include(c => c.CaseLocations)
                 .ThenInclude(c => c.InvestigationCaseSubStatus)
                 .Include(c => c.CaseLocations)
                 .ThenInclude(c => c.PinCode)
                 .Include(c => c.CaseLocations)
                 .ThenInclude(c => c.Vendor)
                 .Include(c => c.PolicyDetail)
                 .ThenInclude(c => c.CostCentre)
                 .Include(c => c.CustomerDetail)
                 .ThenInclude(c => c.Country)
                 .Include(c => c.CustomerDetail)
                 .ThenInclude(c => c.District)
                 .Include(c => c.InvestigationCaseStatus)
                 .Include(c => c.InvestigationCaseSubStatus)
                 .Include(c => c.PolicyDetail)
                 .ThenInclude(c => c.InvestigationServiceType)
                 .Include(c => c.PolicyDetail)
                 .ThenInclude(c => c.LineOfBusiness)
                 .Include(c => c.CustomerDetail)
                 .ThenInclude(c => c.PinCode)
                 .Include(c => c.CustomerDetail)
                 .ThenInclude(c => c.State)
                 .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var claimCase = _context.CaseLocation
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.ClaimReport)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
                && c.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId
            );

            return new ClaimsInvestigationVendorsModel { CaseLocation = claimCase, ClaimsInvestigation = claimsInvestigation };
        }
    }
}