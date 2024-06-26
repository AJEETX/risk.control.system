﻿using System.Security.Claims;
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
using risk.control.system.Controllers.Api.Claims;
using Microsoft.AspNetCore.Hosting;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = ASSESSOR.DISPLAY_NAME)]
    public class AssessorController : ControllerBase
    {
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IClaimsService claimsService;

        public AssessorController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IClaimsService claimsService)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            hindiNFO.CurrencySymbol = string.Empty;
            this.claimsService = claimsService;
        }

        [HttpGet("Get")]
        public IActionResult GetAssessor()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();

            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);
            var replyByAgency = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i =>
            i.ClientCompanyId == companyUser.ClientCompanyId &&
            i.UserEmailActionedTo == string.Empty &&
             i.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}" &&
            i.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId ||
            i.InvestigationCaseSubStatusId == replyByAgency.InvestigationCaseSubStatusId
             );

            var newClaimsAssigned = new List<ClaimsInvestigation>();
            var claimsAssigned = new List<ClaimsInvestigation>();

            foreach (var claim in applicationDbContext)
            {
                claim.AssessView += 1;
                if (claim.AssessView <= 1)
                {
                    newClaimsAssigned.Add(claim);
                }
                claimsAssigned.Add(claim);

            }
            if (newClaimsAssigned.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newClaimsAssigned);
                _context.SaveChanges();
            }
            var response = claimsAssigned
            .Select(a => new ClaimsInvesgationResponse
            {
                Id = a.ClaimsInvestigationId,
                AutoAllocated = a.AutoAllocated,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(hindiNFO, "{0:c}", a.PolicyDetail.SumAssuredValue),
                AssignedToAgency = a.AssignedToAgency,
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail?.CustomerName != null ? a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/user.png\" /> </span>",
                Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                Status = string.Join("", "ORIGIN of Claim: " + a.ORIGIN.GetEnumDisplayName() + ""),
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
                TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                IsNewAssigned = a.AssessView <= 1
            })?.ToList();

            return Ok(response);
        }

        [HttpGet("GetReview")]
        public IActionResult GetReview()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();

            var requestedByAssessor = _context.InvestigationCaseSubStatus
               .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR);

            var claimsSubmitted = new List<ClaimsInvestigation>();
            if (userRole.Value.Contains(AppRoles.CREATOR.ToString()))
            {
                var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();
                var claims = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
                a.ClientCompanyId == companyUser.ClientCompanyId)?.ToList();
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
                    c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);

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
                        reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
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
                        Amount = string.Format(hindiNFO, "{0:c}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                        OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.CustomerName != null ?
                        a.CustomerDetail?.CustomerName : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "ORIGIN of Claim: " + a.ORIGIN.GetEnumDisplayName() + ""),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = !a.NotWithdrawable,
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

        [HttpGet("GetReport")]
        public IActionResult GetReport()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims()
                .Where(c =>
                c.CustomerDetail != null && c.AgencyReport != null);
            var user = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user);
            var approvedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var finishStatus = _context.InvestigationCaseStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);

            applicationDbContext = applicationDbContext.Where(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
                (c.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR &&
                c.InvestigationCaseStatusId == finishStatus.InvestigationCaseStatusId)
                || c.InvestigationCaseSubStatusId == rejectdStatus.InvestigationCaseSubStatusId
                );
            var claimsSubmitted = new List<ClaimsInvestigation>();
            applicationDbContext = applicationDbContext.Where(c => c.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId);

            foreach (var claim in applicationDbContext)
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
                Amount = String.Format(hindiNFO, "{0:C}", a.PolicyDetail.SumAssuredValue),
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
                Status = string.Join("", "ORIGIN of Claim: " + a.ORIGIN.GetEnumDisplayName() + ""),
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
        public IActionResult GetReject()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims()
                .Where(c => c.CustomerDetail != null && c.AgencyReport != null);
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var rejectdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);

            var claims = applicationDbContext.Where(c => c.ClientCompanyId == companyUser.ClientCompanyId &&
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
                Amount = String.Format(hindiNFO, "{0:C}", a.PolicyDetail.SumAssuredValue),
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
                Status = string.Join("", "ORIGIN of Claim: " + a.ORIGIN.GetEnumDisplayName() + ""),
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
        private byte[] GetOwner(ClaimsInvestigation a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            ClientCompany company = null;
            ClientCompanyApplicationUser companyuser = null;
            Vendor vendorOwner = null;
            VendorApplicationUser agent = null;
            string profileImage = string.Empty;
            var allocated2agent = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);

            if (!string.IsNullOrWhiteSpace(a.UserEmailActionedTo) && a.InvestigationCaseSubStatusId == allocated2agent.InvestigationCaseSubStatusId)
            {
                ownerEmail = a.UserEmailActionedTo;
                var agentProfile = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == ownerEmail)?.ProfilePicture;
                if (agentProfile == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return agentProfile;
            }
            else if (string.IsNullOrWhiteSpace(a.UserEmailActionedTo) &&
                !string.IsNullOrWhiteSpace(a.UserRoleActionedTo)
                && a.AssignedToAgency)
            {
                ownerDomain = a.UserRoleActionedTo;
                var vendorImage = _context.Vendor.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (vendorImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return vendorImage;
            }
            else
            {
                ownerDomain = a.UserRoleActionedTo;
                var companyImage = _context.ClientCompany.FirstOrDefault(v => v.Email == ownerDomain)?.DocumentImage;
                if (companyImage == null)
                {
                    var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

                    var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);
                    return noDataimage;
                }
                return companyImage;
            }

        }
    }
}