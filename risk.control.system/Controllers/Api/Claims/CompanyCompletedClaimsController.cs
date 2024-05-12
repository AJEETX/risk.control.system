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
using Microsoft.AspNetCore.Authorization;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Claims
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyCompletedClaimsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IClaimsService claimsService;

        public CompanyCompletedClaimsController(ApplicationDbContext context, IClaimsService claimsService)
        {
            _context = context;
            this.claimsService = claimsService;
        }

        [HttpGet("GetReport")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> GetReport()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims().Where(c =>
                c.CustomerDetail != null && c.BeneficiaryDetail.ClaimReport != null);
            var user = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user);
            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);
            var claims = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR && c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId)
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                );
            var claimsSubmitted = new List<ClaimsInvestigation>();
            claims = claims.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId);

            foreach (var claim in claims)
            {

                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                c.UserEmailActioned == companyUser.Email && c.UserEmailActionedTo == companyUser.Email)?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                }

                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                c.HopCount >= reviewLogCount &&
                c.UserEmailActioned == companyUser.Email);
                if (userHasClaimLog)
                {
                    claimsSubmitted.Add(claim);
                }
            }
            var response =
                claimsSubmitted
            .Select(a => new ClaimsInvesgationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.CustomerName != null ? a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                timePending = a.GetTimePending(),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
            })?.ToList();

            return Ok(response);
        }
        [Authorize(Roles = MANAGER.DISPLAY_NAME)]
        [HttpGet("GetManagerReport")]
        public async Task<IActionResult> GetManagerReport()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims().Where(c =>
                c.CustomerDetail != null && c.BeneficiaryDetail.ClaimReport != null);
            var user = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user);
            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);
            var claims = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR && c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId)
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                );
            var claimsSubmitted = new List<ClaimsInvestigation>();
            claims = claims.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId);

            foreach (var claim in claims)
            {

                claimsSubmitted.Add(claim);
            }
            var response =
                claimsSubmitted
            .Select(a => new ClaimsInvesgationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.CustomerName != null ? a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                timePending = a.GetTimePending(),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
            })?.ToList();

            return Ok(response);
        }
        [HttpGet("GetReject")]
        [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
        public async Task<IActionResult> GetReject()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims().Where(c =>
                c.CustomerDetail != null && c.BeneficiaryDetail.ClaimReport != null);
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var claims = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                )?.ToList();
            var claimsSubmitted = new List<ClaimsInvestigation>();

            foreach (var claim in claims)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                c.UserEmailActioned == companyUser.Email && c.UserEmailActionedTo == companyUser.Email)?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                }

                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                c.HopCount >= reviewLogCount &&
                c.UserEmailActioned == companyUser.Email);
                if (userHasClaimLog)
                {
                    claimsSubmitted.Add(claim);
                }
            }
            var response =
                claimsSubmitted
            .Select(a => new ClaimsInvesgationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.CustomerName != null ? a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                timePending = a.GetTimePending(),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
            })?.ToList();

            return Ok(response);
        }

        [HttpGet("GetManagerReject")]
        [Authorize(Roles = MANAGER.DISPLAY_NAME)]
        public async Task<IActionResult> GetManagerReject()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims().Where(c =>
                c.CustomerDetail != null && c.BeneficiaryDetail.ClaimReport != null);
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            var claims = applicationDbContext.Where(c => c.PolicyDetail.ClientCompanyId == companyUser.ClientCompanyId &&
                c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                )?.ToList();
            var claimsSubmitted = new List<ClaimsInvestigation>();

            foreach (var claim in claims)
            {
                claimsSubmitted.Add(claim);
            }
            var response =
                claimsSubmitted
            .Select(a => new ClaimsInvesgationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = String.Format(new CultureInfo("hi-IN"), "{0:C}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ?
                        string.Join("", "<span class='badge badge-light'>" + a.UserEmailActionedTo + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UserRoleActionedTo + "</span>"),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.CustomerName != null ? a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                timePending = a.GetTimePending(),
                PolicyNum = a.GetPolicyNum(),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.BeneficiaryName) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.BeneficiaryName,
                Agency = a.Vendor?.Name,
                TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds
            })?.ToList();

            return Ok(response);
        }
    }
}