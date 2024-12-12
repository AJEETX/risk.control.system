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
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
        private readonly string noUserImagefilePath = string.Empty;
        private readonly string noDataImagefilePath = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AgentController(ApplicationDbContext context, UserManager<VendorApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, IDashboardService dashboardService)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        [HttpGet("GetNew")]
        public IActionResult GetNew()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var requestedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == currentUserEmail);

            applicationDbContext = applicationDbContext
                    .Include(a => a.PolicyDetail)
                    .ThenInclude(a => a.LineOfBusiness)
                    .Where(i => i.VendorId == vendorUser.VendorId);
            var claims = new List<ClaimsInvestigation>();
            List<ClaimsInvestigation> newAllocateClaims = new List<ClaimsInvestigation>();
            List<ClaimsInvestigation> newInvestigateClaims = new List<ClaimsInvestigation>();
            applicationDbContext = applicationDbContext.Where(a => a.UserEmailActionedTo == currentUserEmail
            && a.InvestigationCaseSubStatusId == assignedToAgentStatus.InvestigationCaseSubStatusId);
            foreach (var claim in applicationDbContext)
            {
                claim.InvestigateView += 1;
                if (claim.InvestigateView <= 1)
                {
                    newInvestigateClaims.Add(claim);
                }
                claims.Add(claim);
            }
            if (newInvestigateClaims.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newInvestigateClaims);
                _context.SaveChanges();
            }


            var response = claims
                   .Select(a => new ClaimsInvestigationAgencyResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(hindiNFO, "{0:C}", a.PolicyDetail.SumAssuredValue),
                       Company = a.ClientCompany.Name,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       AssignedToAgency = a.AssignedToAgency,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Name = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetAgentTimePending(),
                       PolicyNum = a.GetPolicyNumForAgency(requestedStatus.InvestigationCaseSubStatusId),
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                       IsNewAssigned = a.InvestigateView <= 1,
                       IsQueryCase = a.InvestigationCaseSubStatusId == requestedStatus.InvestigationCaseSubStatusId
                   })
                    ?.ToList();

            return Ok(response);
        }

        [HttpGet("GetSubmitted")]
        public IActionResult GetSubmitted()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            var allocatedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var submittedToVendorSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var currentUserEmail = HttpContext.User?.Identity?.Name;

            var agentUser = _context.VendorApplicationUser.Include(u => u.Vendor).FirstOrDefault(c => c.Email == currentUserEmail);
            var submittedToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var userAttendedClaims = _context.InvestigationTransaction.Where(t => (t.UserEmailActioned == agentUser.Email &&
            t.UserRoleActionedTo == $"{agentUser.Vendor.Email}" &&
                           t.InvestigationCaseSubStatusId == submittedToSupervisorStatus.InvestigationCaseSubStatusId))?.Select(c => c.ClaimsInvestigationId).Distinct();

            var claimsSubmitted = new List<ClaimsInvestigation>();
            foreach (var item in applicationDbContext)
            {
                if (userAttendedClaims != null && userAttendedClaims.Contains(item.ClaimsInvestigationId) && item.UserEmailActionedTo != currentUserEmail)
                {
                    claimsSubmitted.Add(item);
                }
            }
            var response = claimsSubmitted
                   .Select(a => new ClaimsInvestigationAgencyResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       PolicyId = a.PolicyDetail.ContractNumber,
                       Amount = string.Format(hindiNFO, "{0:C}", a.PolicyDetail.SumAssuredValue),
                       AssignedToAgency = a.AssignedToAgency,
                       Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Company = a.ClientCompany.Name,
                       Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                       Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Name = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                       Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                       Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                       ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                       Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                       Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                       Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                       timePending = a.GetAgentTimePending(),
                       PolicyNum = a.PolicyDetail.ContractNumber,
                       BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                       BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                       TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                   })
                    ?.ToList();

            return Ok(response);

        }
        private IQueryable<ClaimsInvestigation> GetClaims()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = _context.ClaimsInvestigation
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
               .Include(c => c.Vendor)
                .Where(c => !c.Deleted);
            return applicationDbContext.OrderByDescending(o => o.Created);
        }

    }
}