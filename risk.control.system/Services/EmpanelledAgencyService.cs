using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IEmpanelledAgencyService
    {
        //Task<ClaimsInvestigationVendorsModel> GetEmpanelledVendors(string selectedcase);
        Task<CaseInvestigationVendorsModel> GetEmpanelledVendors(long selectedcase);

        //Task<ClaimsInvestigation> GetAllocateToVendor(string selectedcase);

        //Task<ClaimsInvestigation> GetReAllocateToVendor(string selectedcase);

        //ClaimsInvestigation GetCaseLocation(string id);
    }

    public class EmpanelledAgencyService : IEmpanelledAgencyService
    {
        private readonly ApplicationDbContext _context;

        public EmpanelledAgencyService(ApplicationDbContext context)
        {
            this._context = context;
        }

        //public ClaimsInvestigation GetCaseLocation(string id)
        //{
        //    var claim = _context.ClaimsInvestigation
        //     .Include(c => c.PolicyDetail)
        //     .Include(c => c.ClientCompany)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.CaseEnabler)
             
        //     .Include(c => c.BeneficiaryDetail)
        //     .ThenInclude(c => c.PinCode)
        //     .Include(c => c.BeneficiaryDetail)
        //     .ThenInclude(c => c.BeneficiaryRelation)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.CostCentre)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.Country)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.District)
        //     .Include(c => c.InvestigationCaseStatus)
        //     .Include(c => c.InvestigationCaseSubStatus)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.InvestigationServiceType)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.LineOfBusiness)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.PinCode)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.State)
        //     .FirstOrDefault(a => a.ClaimsInvestigationId == id);
        //    return claim;
        //}

        //public async Task<ClaimsInvestigation> GetAllocateToVendor(string selectedcase)
        //{
        //    var claimsInvestigation = await _context.ClaimsInvestigation
        //     .Include(c => c.PolicyDetail)
        //     .Include(c => c.ClientCompany)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.CaseEnabler)
            
        //     .Include(c => c.BeneficiaryDetail)
        //     .ThenInclude(c => c.PinCode)
        //     .Include(c => c.BeneficiaryDetail)
        //     .ThenInclude(c => c.BeneficiaryRelation)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.CostCentre)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.Country)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.District)
        //     .Include(c => c.InvestigationCaseStatus)
        //     .Include(c => c.InvestigationCaseSubStatus)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.InvestigationServiceType)
        //     .Include(c => c.PolicyDetail)
        //     .ThenInclude(c => c.LineOfBusiness)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.PinCode)
        //     .Include(c => c.CustomerDetail)
        //     .ThenInclude(c => c.State)
        //     .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

        //    var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
        //        i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            
        //    return claimsInvestigation;
        //}

        //public async Task<ClaimsInvestigationVendorsModel> GetEmpanelledVendors(string selectedcase)
        //{
        //    var claimsInvestigation = await _context.ClaimsInvestigation
        //        .Include(c => c.PolicyDetail)
        //        .Include(c => c.ClientCompany)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.CaseEnabler)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.CostCentre)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.Country)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.District)
        //        .Include(c => c.InvestigationCaseStatus)
        //        .Include(c => c.InvestigationCaseSubStatus)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.InvestigationServiceType)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.LineOfBusiness)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.PinCode)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.State)
        //        .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

        //    var claimCase = _context.BeneficiaryDetail
        //        .Include(c => c.ClaimsInvestigation)
        //        .Include(c => c.PinCode)
        //        .Include(c => c.BeneficiaryRelation)
        //        .Include(c => c.District)
        //        .Include(c => c.State)
        //        .Include(c => c.Country)
        //        .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase
        //        );


        //    return new ClaimsInvestigationVendorsModel { 
        //        Location = claimCase, 
        //        //Vendors = vendorWithCaseCounts, 
        //        ClaimsInvestigation = claimsInvestigation };
        //}
        public async Task<CaseInvestigationVendorsModel> GetEmpanelledVendors(long selectedcase)
        {
            var claimsInvestigation = await _context.Investigations
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
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.LineOfBusiness)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(m => m.Id == selectedcase);
            var claimCase = _context.BeneficiaryDetail
               .Include(c => c.PinCode)
               .Include(c => c.BeneficiaryRelation)
               .Include(c => c.District)
               .Include(c => c.State)
               .Include(c => c.Country)
               .FirstOrDefault(c => c.InvestigationTaskId == selectedcase );
            return new CaseInvestigationVendorsModel
            {
                Location = claimCase,
                //Vendors = vendorWithCaseCounts, 
                ClaimsInvestigation = claimsInvestigation
            };
        }

        //public async Task<ClaimsInvestigation> GetReAllocateToVendor(string selectedcase)
        //{
        //    var claimsInvestigation = await _context.ClaimsInvestigation
        //        .Include(c => c.PolicyDetail)
        //        .Include(c => c.ClientCompany)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.CaseEnabler)
                
        //        .Include(c => c.BeneficiaryDetail)
        //        .ThenInclude(c => c.PinCode)
        //        .Include(c => c.BeneficiaryDetail)
        //        .ThenInclude(c => c.BeneficiaryRelation)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.CostCentre)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.Country)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.District)
        //        .Include(c => c.InvestigationCaseStatus)
        //        .Include(c => c.InvestigationCaseSubStatus)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.InvestigationServiceType)
        //        .Include(c => c.PolicyDetail)
        //        .ThenInclude(c => c.LineOfBusiness)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.PinCode)
        //        .Include(c => c.CustomerDetail)
        //        .ThenInclude(c => c.State)
        //        .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

        //    var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
        //        i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
        //    return claimsInvestigation;
        //}
    }
}