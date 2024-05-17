using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;
using risk.control.system.Services;
using System.Globalization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Claims
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyActiveClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IClaimsService claimsService;

        public CompanyActiveClaimsController(ApplicationDbContext context, IClaimsService claimsService)
        {
            _context = context;
            this.claimsService = claimsService;
        }

        [HttpGet("GetActive")]
        public async Task<IActionResult> GetActive()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.Include(u=>u.ClientCompany).FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                     i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var reAssignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var approvedStatus = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();

            var claims = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.InvestigationCaseStatusId != approvedStatus.InvestigationCaseSubStatusId &&
            a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);
            List<ClaimsInvestigation> newClaims = new List<ClaimsInvestigation>();
            foreach (var claim in claims)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                c.UserRoleActionedTo == $"{AppRoles.CREATOR.GetEnumDisplayName()} ( {companyUser.ClientCompany.Email})")?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o=>o.HopCount).First().HopCount;
                }
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && 
                c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);

                if (userHasClaimLog && claim.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId &&
                    claim.InvestigationCaseSubStatusId != assignedToAssignerStatus.InvestigationCaseSubStatusId &&
                    claim.InvestigationCaseSubStatusId != withdrawnByAgency.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != reAssignedToAssignerStatus.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != approvedStatus.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != withdrawnByCompany.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != approvedStatus.InvestigationCaseSubStatusId
                    )
                {
                    claim.ActiveView += 1;
                    if(claim.ActiveView <= 1)
                    {
                        newClaims.Add(claim);
                    }
                    claimsSubmitted.Add(claim);
                }
            }
            if(newClaims.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newClaims);
                _context.SaveChanges();
            }
            
            var response = claimsSubmitted
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.CustomerName) ? "" : a.CustomerDetail.CustomerName,
                        BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ? "" : a.BeneficiaryDetail.BeneficiaryName,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ?
                        a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "(" + a.PolicyDetail.InvestigationServiceType.Name + ")</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ? true : false,
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                        IsNewAssigned = a.ActiveView <= 1
                    })?
                    .ToList();

            return Ok(response);
        }

        [HttpGet("GetManagerActive")]
        public async Task<IActionResult> GetManagerActive()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                     i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var reAssignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                         i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var approvedStatus = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var claimsSubmitted = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();

            var claims = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.InvestigationCaseStatusId != approvedStatus.InvestigationCaseSubStatusId);
            List<ClaimsInvestigation> newClaims = new List<ClaimsInvestigation>();
            foreach (var claim in claims)
            {
                if (claim.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId &&
                    claim.InvestigationCaseSubStatusId != withdrawnByAgency.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != reAssignedToAssignerStatus.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != approvedStatus.InvestigationCaseSubStatusId
                    &&
                     claim.InvestigationCaseSubStatusId != submittededToAssesssorStatus.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != withdrawnByCompany.InvestigationCaseSubStatusId
                    &&
                    claim.InvestigationCaseSubStatusId != approvedStatus.InvestigationCaseSubStatusId
                    )
                {
                    claim.ManagerActiveView += 1;
                    if (claim.ManagerActiveView <= 1)
                    {
                        newClaims.Add(claim);
                    }
                    claimsSubmitted.Add(claim);
                }
            }
            if (newClaims.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newClaims);
                _context.SaveChanges();
            }

            var response = claimsSubmitted
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.CustomerName) ? "" : a.CustomerDetail.CustomerName,
                        BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ? "" : a.BeneficiaryDetail.BeneficiaryName,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ?
                        a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "(" + a.PolicyDetail.InvestigationServiceType.Name + ")</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ? true : false,
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                        IsNewAssigned = a.ManagerActiveView <= 1
                    })?
                    .ToList();
            return Ok(response);
        }
        [HttpGet("GetIncomplete")]
        public async Task<IActionResult> GetIncomplete()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            var newClaims = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            var claims = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) && a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
            foreach (var claim in claims)
            {
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.UserEmailActioned == companyUser.Email);
                if (userHasClaimLog && !claim.AssignedToAgency && claim.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId && !claim.IsReady2Assign)
                {
                    claim.DraftView += 1;
                    if(claim.DraftView <=1)
                    {
                        newClaims.Add(claim);
                    }
                    claimsSubmitted.Add(claim);
                }
            }
            if(newClaims.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newClaims);
                _context.SaveChanges();
            }
            var response = claimsSubmitted
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.CustomerName) ? "" : a.CustomerDetail.CustomerName,
                        BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ? "" : a.BeneficiaryDetail?.BeneficiaryName,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ?
                        a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ? true : false,
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto =  a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail?.BeneficiaryName,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                        IsNewAssigned = a.DraftView <= 1
                    })?
                    .ToList();

            return Ok(response);
        }
        [HttpGet("GetReview")]
        public async Task<IActionResult> GetReview()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var requestedByAssessor = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            if (userRole.Value.Contains(AppRoles.CREATOR.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                var claims = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) && 
                a.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
                foreach (var claim in claims)
                {
                    var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                        c.UserEmailActioned == companyUser.Email)?.ToList();

                    int? reviewLogCount = 0;
                    if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                    {
                        reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                    }
                    var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                    c.UserEmailActioned == companyUser.Email &&  c.HopCount >= reviewLogCount);

                    if (userHasClaimLog)
                    {
                        claimsSubmitted.Add(claim);
                    }
                }
            }

            else if (userRole.Value.Contains(AppRoles.ASSESSOR.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId)
                );

                foreach (var claim in applicationDbContext)
                {
                    var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && 
                    c.IsReviewCase && c.UserEmailActioned == companyUser.Email)?.ToList();

                    int? reviewLogCount = 0;
                    if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                    {
                        reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o=>o.HopCount).First().HopCount;
                    }
                    var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && 
                    c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);

                    if (claim.IsReviewCase && userHasClaimLog || claim.InvestigationCaseSubStatusId == requestedByAssessor.InvestigationCaseSubStatusId)
                    {
                        claimsSubmitted.Add(claim);
                    }
                }
            }
            var response = claimsSubmitted
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.CustomerName) ? "" : a.CustomerDetail.CustomerName,
                        BeneficiaryFullName = a.BeneficiaryDetail is null ? "" : a.BeneficiaryDetail.BeneficiaryName,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ?
                        a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ? true : false,
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                    })?
                    .ToList();

            return Ok(response);
        }

        [HttpGet("GetManagerReview")]
        public async Task<IActionResult> GetManagerReview()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
            applicationDbContext = applicationDbContext.Where(a =>
            openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.VendorId != null);

            foreach (var claim in applicationDbContext)
            {
                if (claim.IsReviewCase && claim.ReviewCount == 1)
                {
                    claimsSubmitted.Add(claim);
                }
            }
            var response = claimsSubmitted
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.CustomerName) ? "" : a.CustomerDetail.CustomerName,
                        BeneficiaryFullName = a.BeneficiaryDetail is null ? "" : a.BeneficiaryDetail.BeneficiaryName,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ?
                        a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = a.InvestigationCaseSubStatusId == allocateToVendorStatus.InvestigationCaseSubStatusId ? true : false,
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
                    })?
                    .ToList();

            return Ok(response);
        }

        [HttpGet("GetActiveMap")]
        public async Task<IActionResult> GetActiveMap()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId);

            var company = _context.ClientCompany.Include(c => c.PinCode).FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var assignedToAssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var allocateToVendorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            var submittededToSupervisorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);

            var submittededToAssesssorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var reAssigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var claimsSubmitted = new List<ClaimsInvestigation>();

            if (userRole.Value.Contains(AppRoles.CREATOR.ToString()) || userRole.Value.Contains(AppRoles.COMPANY_ADMIN.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId));
                claimsSubmitted = await applicationDbContext.ToListAsync();
            }
            
            else if (userRole.Value.Contains(AppRoles.ASSESSOR.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                applicationDbContext = applicationDbContext.Where(a =>
                openStatusesIds.Contains(a.InvestigationCaseStatusId)
                );

                foreach (var item in applicationDbContext)
                {
                    if ((item.InvestigationCaseSubStatusId == submittededToAssesssorStatus.InvestigationCaseSubStatusId) ||
                        (item.IsReviewCase))
                    {
                        claimsSubmitted.Add(item);
                    }
                }
            }

            var response = claimsSubmitted
                   .Select(a => new MapResponse
                   {
                       Id = a.ClaimsInvestigationId,
                       Address = LocationDetail.GetAddress(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                       Description = a.PolicyDetail.CauseOfLoss,
                       Price = a.PolicyDetail.SumAssuredValue,
                       Type = a.PolicyDetail.ClaimType == ClaimType.HEALTH ? "home" : "building",
                       Bed = a.CustomerDetail?.CustomerIncome.GetEnumDisplayName(),
                       Bath = a.CustomerDetail?.ContactNumber,
                       Size = a.CustomerDetail?.Description,
                       Position = new Position
                       {
                           Lat = claimsService.GetLat(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail) ?? decimal.Parse(company.PinCode.Latitude),
                           Lng = claimsService.GetLng(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail) ?? decimal.Parse(company.PinCode.Longitude),
                       },
                       Url = (a.BeneficiaryDetail != null) ? "/ClaimsInvestigation/Detail?Id=" + a.ClaimsInvestigationId : "/ClaimsInvestigation/Details?Id=" + a.ClaimsInvestigationId
                   })?
                   .ToList();

            foreach (var item in response)
            {
                var isExist = response.Any(r => r.Position.Lng == item.Position.Lng && r.Position.Lat == item.Position.Lat && item.Id != r.Id);
                if (isExist)
                {
                    var (lat, lng) = LocationDetail.GetLatLng(item.Position.Lat, item.Position.Lng);
                    item.Position = new Position
                    {
                        Lat = lat,
                        Lng = lng,
                    };
                }
            }
            return Ok(new
            {
                response = response,
                lat = decimal.Parse(company.PinCode.Latitude),
                lng = decimal.Parse(company.PinCode.Longitude)
            });
        }
    }
}