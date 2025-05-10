using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/agency/[controller]")]
    [ApiController]
    [Authorize(Roles = AGENT.DISPLAY_NAME)]
    public class AgentController : ControllerBase
    {
        private const string UNDERWRITING = "underwriting";
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
        private readonly string noUserImagefilePath = string.Empty;
        private readonly string noDataImagefilePath = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgentController(ApplicationDbContext context, 
            UserManager<VendorApplicationUser> userManager, 
            IWebHostEnvironment webHostEnvironment, 
            IDashboardService dashboardService)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        [HttpGet("GetNew")]
        public async Task<IActionResult> GetNew()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.Include(v => v.Country).FirstOrDefault(c => c.Email == currentUserEmail);
            var assignedToAgentStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
            var claims = await GetClaims()
                    .Where(i => i.VendorId == vendorUser.VendorId &&  
                    i.TaskedAgentEmail == currentUserEmail && 
                    !i.Deleted && 
                    i.SubStatus == assignedToAgentStatus).ToListAsync();
            

            var response = claims
                   .Select(a => new ClaimsInvestigationAgencyResponse
                   {
                       Id = a.Id,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       Company = a.ClientCompany.Name,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       AssignedToAgency = a.AssignedToAgency,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                       Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                       Status = a.SubStatus,
                       ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                       Service = a.PolicyDetail.InvestigationServiceType.Name,
                       Location = a.SubStatus,
                       Created = a.Created.ToString("dd-MM-yyyy"),
                       timePending = a.GetAgentTimePending(),
                       PolicyNum = a.GetPolicyNumForAgency(CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR),
                       BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                       TimeElapsed = DateTime.Now.Subtract(a.TaskToAgentTime.Value).TotalSeconds,
                       IsNewAssigned = a.IsNewSubmittedToAgent,
                       IsQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
                       PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                       Distance = a.SelectedAgentDrivingDistance,
                       Duration = a.SelectedAgentDrivingDuration
                   })
                    ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetSubmitted")]
        public async Task<IActionResult> GetSubmitted()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var agentUser = _context.VendorApplicationUser.Include(v=>v.Country).Include(u => u.Vendor).FirstOrDefault(c => c.Email == currentUserEmail);
            var claims = await GetClaims()
                    .Where(i => i.VendorId == agentUser.VendorId &&
                    i.TaskedAgentEmail == currentUserEmail &&
                    !i.Deleted &&
                    i.SubStatus != CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT).ToListAsync();

            var response = claims
                   .Select(a => new ClaimsInvestigationAgencyResponse
                   {
                       Id = a.Id,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(Extensions.GetCultureByCountry(agentUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                       AssignedToAgency = a.AssignedToAgency,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.ClientCompany.Name,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                       Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                       Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                       Status = a.SubStatus,
                       ServiceType = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                       Service = a.PolicyDetail.InvestigationServiceType.Name,
                       Location = a.SubStatus,
                       Created = a.Created.ToString("dd-MM-yyyy"),
                       timePending = a.GetAgentTimePending(true),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                       TimeElapsed = DateTime.Now.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds,
                       PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                       Distance = a.SelectedAgentDrivingDistance,
                       Duration = a.SelectedAgentDrivingDuration
                   })
                    ?.ToList();

            return Ok(response);

        }
        private IQueryable<InvestigationTask> GetClaims()
        {
            IQueryable<InvestigationTask> applicationDbContext = _context.Investigations
               .Include(c => c.PolicyDetail)
               .Include(c => c.ClientCompany)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(b => b.BeneficiaryRelation)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CaseEnabler)
               .Include(c => c.PolicyDetail)
               .ThenInclude(c => c.CostCentre)
               .Include(c => c.BeneficiaryDetail)
               .ThenInclude(c => c.PinCode)
               .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(c => c.State)
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
               .Include(c => c.Vendor)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }

    }
}