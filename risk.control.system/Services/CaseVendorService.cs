using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Controllers.Api.Claims;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICaseVendorService
    {
        Task<ClaimsInvestigation> AllocateToVendorAgent(string userEmail, string selectedcase);

        Task<ClaimsInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, string selectedcase);

        Task<ClaimsInvestigationVendorAgentModel> ReSelectVendorAgent(string userEmail, string selectedcase);

        Task<ClaimsInvestigationVendorsModel> GetInvestigate(string userEmail, string selectedcase, bool uploaded = false);

        Task<ClaimsInvestigationVendorsModel> GetInvestigateReport(string userEmail, string selectedcase);

        Task<ClaimTransactionModel> GetClaimsDetails(string userEmail, string selectedcase);
    }

    public class CaseVendorService : ICaseVendorService
    {
        private const string CLAIMS = "claims";
        private const string UNDERWRITING = "underwriting";
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly ApplicationDbContext _context;
        private readonly IDashboardService dashboardService;
        private readonly IFeatureManager featureManager;
        private readonly IClaimsService claimsService;

        //private static string latitude = "-37.839542";
        //private static string longitude = "145.164834";

        public CaseVendorService(
            UserManager<VendorApplicationUser> userManager,
            ApplicationDbContext context,
            IDashboardService dashboardService,
            IFeatureManager featureManager,
            IClaimsService claimsService)
        {
            this.userManager = userManager;
            this._context = context;
            this.dashboardService = dashboardService;
            this.featureManager = featureManager;
            this.claimsService = claimsService;
        }
        public async Task<ClaimsInvestigation> AllocateToVendorAgent(string userEmail, string selectedcase)
        {
            var vendorUser =await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var claimsInvestigation = claimsService.GetClaims().FirstOrDefault(m => m.ClaimsInvestigationId == selectedcase && m.VendorId == vendorUser.VendorId);

            return claimsInvestigation;
        }

        public async Task<ClaimsInvestigationVendorAgentModel> SelectVendorAgent(string userEmail, string selectedcase)
        {
            var claimsAllocate2Agent = claimsService.GetClaims().Include(c=>c.AgencyReport).FirstOrDefault(v => v.ClaimsInvestigationId == selectedcase);
            
            var beneficiaryDetail = await _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.BeneficiaryDetailId == claimsAllocate2Agent.BeneficiaryDetail.BeneficiaryDetailId);

            var maskedCustomerContact = new string('*', claimsAllocate2Agent.CustomerDetail.ContactNumber.ToString().Length - 4) + claimsAllocate2Agent.CustomerDetail.ContactNumber.ToString().Substring(claimsAllocate2Agent.CustomerDetail.ContactNumber.ToString().Length - 4);
            claimsAllocate2Agent.CustomerDetail.ContactNumber = maskedCustomerContact;
            var maskedBeneficiaryContact = new string('*', beneficiaryDetail.ContactNumber.ToString().Length - 4) + beneficiaryDetail.ContactNumber.ToString().Substring(beneficiaryDetail.ContactNumber.ToString().Length - 4);
            claimsAllocate2Agent.BeneficiaryDetail.ContactNumber = maskedBeneficiaryContact;
            beneficiaryDetail.ContactNumber = maskedBeneficiaryContact;
            
            var model = new ClaimsInvestigationVendorAgentModel
            {
                CaseLocation = beneficiaryDetail,
                ClaimsInvestigation = claimsAllocate2Agent,
            };
            return model;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetInvestigate(string userEmail, string selectedcase, bool uploaded = false)
        {
            var claim = claimsService.GetClaims()
                .Include(c => c.ClaimNotes)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.AgentIdReport)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.DigitalIdReport)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.PanIdReport)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.PassportIdReport)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.AudioReport)
                .Include(c => c.AgencyReport)
                .ThenInclude(c => c.VideoReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);

            var customerContactMasked = new string('*', claim.CustomerDetail.ContactNumber.ToString().Length - 4) + claim.CustomerDetail.ContactNumber.ToString().Substring(claim.CustomerDetail.ContactNumber.ToString().Length - 4);
            claim.CustomerDetail.ContactNumber = customerContactMasked;

            var beneficairyContactMasked = new string('*', claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4) + claim.BeneficiaryDetail.ContactNumber.ToString().Substring(claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4);

            claim.BeneficiaryDetail.ContactNumber = beneficairyContactMasked;

            var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;

            ClaimsInvestigationVendorsModel model = null;
            if (claim.AgencyReport == null)
            {
                claim.AgencyReport = new AgencyReport();
            }

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date and time of death ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }

            claim.AgencyReport.AgentEmail = userEmail;
            model = new ClaimsInvestigationVendorsModel { AgencyReport = claim.AgencyReport, Location = claim.BeneficiaryDetail, ClaimsInvestigation = claim };

            _context.ClaimsInvestigation.Update(claim);
            var rows =await _context.SaveChangesAsync();
            return model;
        }

        public async Task<ClaimsInvestigationVendorsModel> GetInvestigateReport(string userEmail, string selectedcase)
        {
            var claim = claimsService.GetClaims()
                .Include(c => c.ClaimMessages)
                .Include(c => c.AgencyReport)
                .ThenInclude(c=>c.EnquiryRequest)
                .Include(c => c.AgencyReport.AgentIdReport)
                .Include(c => c.AgencyReport.DigitalIdReport)
                .Include(c => c.AgencyReport.ReportQuestionaire)
                .Include(c => c.AgencyReport.PanIdReport)
                .Include(c => c.AgencyReport.PassportIdReport)
                .Include(c => c.AgencyReport.AudioReport)
                .Include(c => c.AgencyReport.VideoReport)
                .FirstOrDefault(c => c.ClaimsInvestigationId == selectedcase);


            var beneficiaryDetails =await _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.Country)
                .Include(c => c.State)
                .FirstOrDefaultAsync(c => c.ClaimsInvestigationId == selectedcase);
            var customerContactMasked = new string('*', claim.CustomerDetail.ContactNumber.ToString().Length - 4) + claim.CustomerDetail.ContactNumber.ToString().Substring(claim.CustomerDetail.ContactNumber.ToString().Length - 4);
            claim.CustomerDetail.ContactNumber = customerContactMasked;

            var beneficairyContactMasked = new string('*', claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4) + claim.BeneficiaryDetail.ContactNumber.ToString().Substring(claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4);

            claim.BeneficiaryDetail.ContactNumber = beneficairyContactMasked;

            beneficiaryDetails.ContactNumber = beneficairyContactMasked;
            if (claim.IsReviewCase)
            {
                claim.AgencyReport.SupervisorRemarks = null;
            }
            var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date and time of death ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }

            return (new ClaimsInvestigationVendorsModel { AgencyReport = claim.AgencyReport, Location = beneficiaryDetails, ClaimsInvestigation = claim });
        }

        public async Task<ClaimTransactionModel> GetClaimsDetails(string userEmail, string selectedcase)
        {
            var agencyUser =await _context.VendorApplicationUser.FirstOrDefaultAsync(u=>u.Email == userEmail);

            var claim = claimsService.GetClaims()
                .Include(c=>c.Vendor)
                .Include(c=>c.AgencyReport)
                .ThenInclude(c=>c.EnquiryRequest)
                .Include(c=>c.AgencyReport.AgentIdReport)
                .Include(c=>c.AgencyReport.DigitalIdReport)
                .Include(c=>c.AgencyReport.PanIdReport)
                .Include(c=>c.AgencyReport.PassportIdReport)
                .Include(c=>c.AgencyReport.AudioReport)
                .Include(c=>c.AgencyReport.VideoReport)
                .Include(c=>c.AgencyReport.ReportQuestionaire)
                .Include(c=>c.ClaimNotes)
                .Include(c=>c.ClaimMessages)
                .FirstOrDefault(m => m.ClaimsInvestigationId == selectedcase);

            var customerContactMasked = new string('*', claim.CustomerDetail.ContactNumber.ToString().Length - 4) + claim.CustomerDetail.ContactNumber.ToString().Substring(claim.CustomerDetail.ContactNumber.ToString().Length - 4);
            claim.CustomerDetail.ContactNumber = customerContactMasked;

            var beneficairyContactMasked = new string('*', claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4) + claim.BeneficiaryDetail.ContactNumber.ToString().Substring(claim.BeneficiaryDetail.ContactNumber.ToString().Length - 4);

            claim.BeneficiaryDetail.ContactNumber = beneficairyContactMasked;
            var isClaim = claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM;

            if (isClaim)
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Injury/Illness prior to commencement/revival ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Duration of treatment ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of person met at the cemetery ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date and time of death ?";
            }
            else
            {
                claim.AgencyReport.ReportQuestionaire.Question1 = "Ownership of residence ?";
                claim.AgencyReport.ReportQuestionaire.Question2 = "Perceived financial status ?";
                claim.AgencyReport.ReportQuestionaire.Question3 = "Name of neighbour met ?";
                claim.AgencyReport.ReportQuestionaire.Question4 = "Date when met with neighbour ?";
            }
            claim.AgencyDeclineComment = string.Empty;
            return new ClaimTransactionModel
            {
                ClaimsInvestigation = claim,
                Location = claim.BeneficiaryDetail,
                NotWithdrawable = claim.NotDeclinable,
            };
        }

        public async Task<ClaimsInvestigationVendorAgentModel> ReSelectVendorAgent(string userEmail, string selectedcase)
        {
            var submittedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
            i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var claimsCaseToAllocateToVendorAgent = claimsService.GetClaims().FirstOrDefault(v => v.ClaimsInvestigationId == selectedcase);

            var location = claimsCaseToAllocateToVendorAgent.BeneficiaryDetail;

            var claimsCaseLocation = _context.BeneficiaryDetail
                .Include(c => c.ClaimsInvestigation)
                .Include(c => c.PinCode)
                .Include(c => c.BeneficiaryRelation)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefault(c => c.BeneficiaryDetailId == location.BeneficiaryDetailId);

            var agentRole = _context.ApplicationRole.FirstOrDefault(r => r.Name.Contains(AppRoles.AGENT.ToString()));

            var vendorUsers = _context.VendorApplicationUser
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .Include(u => u.PinCode)
                .Where(u => u.VendorId == claimsCaseToAllocateToVendorAgent.VendorId && u.Active);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var vendorUser in vendorUsers)
            {
                var isTrue = await userManager.IsInRoleAsync(vendorUser, agentRole?.Name);
                if (isTrue)
                {
                    int claimCount = 0;
                    if (result.TryGetValue(vendorUser.Email, out claimCount))
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = claimCount,
                        };
                        agents.Add(agentData);
                    }
                    else
                    {
                        var agentData = new VendorUserClaim
                        {
                            AgencyUser = vendorUser,
                            CurrentCaseCount = 0,
                        };
                        agents.Add(agentData);
                    }
                }
            }

            var model = new ClaimsInvestigationVendorAgentModel
            {
                CaseLocation = claimsCaseLocation,
                ClaimsInvestigation = claimsCaseToAllocateToVendorAgent,
                VendorUserClaims = agents
            };

            claimsCaseToAllocateToVendorAgent.AgencyReport = null;

            _context.SaveChanges();

            return model;
        }
    }
}