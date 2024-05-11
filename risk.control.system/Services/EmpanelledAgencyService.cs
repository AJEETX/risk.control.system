using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IEmpanelledAgencyService
    {
        Task<ClaimsInvestigationVendorsModel> GetEmpanelledVendors(string selectedcase);

        Task<ClaimsInvestigation> GetAllocateToVendor(string selectedcase);

        Task<ClaimsInvestigation> GetReAllocateToVendor(string selectedcase);

        ClaimsInvestigation GetCaseLocation(string id);
    }

    public class EmpanelledAgencyService : IEmpanelledAgencyService
    {
        private readonly ApplicationDbContext _context;

        public EmpanelledAgencyService(ApplicationDbContext context)
        {
            this._context = context;
        }

        public ClaimsInvestigation GetCaseLocation(string id)
        {
            var claim = _context.ClaimsInvestigation
             .Include(c => c.PolicyDetail)
             .ThenInclude(c => c.ClientCompany)
             .Include(c => c.PolicyDetail)
             .ThenInclude(c => c.CaseEnabler)
             
             .Include(c => c.BeneficiaryDetail)
             .ThenInclude(c => c.PinCode)
             .Include(c => c.BeneficiaryDetail)
             .ThenInclude(c => c.BeneficiaryRelation)
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
             .FirstOrDefault(a => a.ClaimsInvestigationId == id);
            return claim;
        }

        public async Task<ClaimsInvestigation> GetAllocateToVendor(string selectedcase)
        {
            var claimsInvestigation = await _context.ClaimsInvestigation
             .Include(c => c.PolicyDetail)
             .ThenInclude(c => c.ClientCompany)
             .Include(c => c.PolicyDetail)
             .ThenInclude(c => c.CaseEnabler)
            
             .Include(c => c.BeneficiaryDetail)
             .ThenInclude(c => c.PinCode)
             .Include(c => c.BeneficiaryDetail)
             .ThenInclude(c => c.BeneficiaryRelation)
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
             .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            
            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetEmpanelledVendors(string selectedcase)
        {
            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
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
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

            var location = claimsInvestigation.BeneficiaryDetail?.BeneficiaryDetailId;

            var claimCase = _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.BeneficiaryDetailId == location
                //&& c.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId
                );

            var existingVendors = await _context.Vendor
                .Where(c => c.Clients.Any(c => c.ClientCompanyId == claimCase.ClaimsInvestigation.PolicyDetail.ClientCompanyId))
                .Include(v => v.ratings)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)?
                .ToListAsync();
            if (claimsInvestigation.IsReviewCase)
            {
                existingVendors = existingVendors.Where(v => v.VendorId != claimsInvestigation.VendorId)?.ToList();
            }

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submitted2SuperStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claimsCases = _context.ClaimsInvestigation
                .Include(c=>c.BeneficiaryDetail)
                .Where(c => 
                (c.InvestigationCaseSubStatusId == allocatedStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId ||
                                    c.InvestigationCaseSubStatusId == submitted2SuperStatus.InvestigationCaseSubStatusId)
                )?.ToList();

            var vendorCaseCount = new Dictionary<string, int>();

            int countOfCases = 0;
            foreach (var claimsCase in claimsCases)
            {
                if (claimsCase.BeneficiaryDetail.BeneficiaryDetailId > 0)
                {
                    if (claimsCase.VendorId.HasValue)
                    {
                        if (!vendorCaseCount.TryGetValue(claimsCase.VendorId.ToString(), out countOfCases))
                        {
                            vendorCaseCount.Add(claimsCase.VendorId.ToString(), 1);
                        }
                        else
                        {
                            int currentCount = vendorCaseCount[claimsCase.VendorId.ToString()];
                            ++currentCount;
                            vendorCaseCount[claimsCase.VendorId.ToString()] = currentCount;
                        }
                    }
                }
            }

            List<VendorCaseModel> vendorWithCaseCounts = new();

            foreach (var existingVendor in existingVendors)
            {
                var vendorCase = vendorCaseCount.FirstOrDefault(v => v.Key == existingVendor.VendorId.ToString());
                if (vendorCase.Key == existingVendor.VendorId.ToString())
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = vendorCase.Value,
                        Vendor = existingVendor,
                    });
                }
                else
                {
                    vendorWithCaseCounts.Add(new VendorCaseModel
                    {
                        CaseCount = 0,
                        Vendor = existingVendor,
                    });
                }
            }

            return new ClaimsInvestigationVendorsModel { Location = claimCase, Vendors = vendorWithCaseCounts, ClaimsInvestigation = claimsInvestigation };
        }

        public async Task<ClaimsInvestigation> GetReAllocateToVendor(string selectedcase)
        {
            var claimsInvestigation = await _context.ClaimsInvestigation
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(c => c.CaseEnabler)
                
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.BeneficiaryRelation)
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
                .FirstOrDefaultAsync(m => m.ClaimsInvestigationId == selectedcase);

            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            return claimsInvestigation;
        }
    }
}