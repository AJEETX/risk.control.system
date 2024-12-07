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
using System.Text;
using Microsoft.AspNetCore.Hosting;
using risk.control.system.Controllers.Api.Claims;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    public class CreatorController : ControllerBase
    {
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IClaimsService claimsService;

        public CreatorController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IClaimsService claimsService)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.claimsService = claimsService;
        }
        [HttpGet("GetAuto")]
        public IActionResult GetAuto()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims();

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail.Value);

            applicationDbContext = applicationDbContext.Where(a =>
                a.ClientCompanyId == companyUser.ClientCompanyId &&
                     a.UserEmailActioned == companyUser.Email &&
                         a.UserEmailActionedTo == companyUser.Email &&
                         a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId
                 );

            var claimsAssigned = new List<ClaimsInvestigation>();
            var newClaimsAssigned = new List<ClaimsInvestigation>();
            foreach (var item in applicationDbContext)
            {
                item.AutoNew += 1;
                if (item.AutoNew <= 1)
                {
                    newClaimsAssigned.Add(item);
                }
                claimsAssigned.Add(item);
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
                    Amount = String.Format(hindiNFO, "{0:C}", a.PolicyDetail.SumAssuredValue),
                    PolicyId = a.PolicyDetail.ContractNumber,
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
                    Name = a.CustomerDetail?.Name != null ?
                    a.CustomerDetail?.Name : "<span class=\"badge badge-light\"> <i class=\"fas fa-question\" ></i>  </span>",
                    Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                    Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                    SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                    Ready2Assign = a.IsReady2Assign,
                    ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                    Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                    Location = string.Join("", "<span class='badge badge-light'>" + a.ORIGIN.GetEnumDisplayName() + "</span>"),
                    Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                    timePending = a.GetTimePending(),
                    Withdrawable = !a.NotWithdrawable,
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-light\"> <i class=\"fas fa-question\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                    IsNewAssigned = a.AutoNew <= 1,
                    BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                    CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,

                })?
                .ToList();

            return Ok(response);
        }
        [HttpGet("GetManual")]
        public IActionResult GetManual()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims().Include(a =>a.PreviousClaimReports);

            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
             i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);

            var assignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var reAssignedStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(i =>
                i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);

            var withdrawnByAgency = _context.InvestigationCaseSubStatus.FirstOrDefault(
                      i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var withdrawnByCompany = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == userEmail.Value);

            applicationDbContext = applicationDbContext.Where(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
                 (a.InvestigationCaseSubStatusId == withdrawnByAgency.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == string.Empty &&
                        a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                 ||
                 (a.InvestigationCaseSubStatusId == withdrawnByCompany.InvestigationCaseSubStatusId &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.UserEmailActioned == companyUser.Email &&
                        a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                 ||
                 (a.UserEmailActioned == companyUser.Email &&
                        a.UserEmailActionedTo == companyUser.Email &&
                        a.InvestigationCaseSubStatusId == assignedStatus.InvestigationCaseSubStatusId)
                        ||
                        (!companyUser.ClientCompany.AutoAllocation && a.UserEmailActioned == companyUser.Email &&
                         a.UserEmailActionedTo == companyUser.Email &&
                         a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId) ||
                (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId &&
                a.UserEmailActionedTo == string.Empty &&
                a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                );

            var claimsAssigned = new List<ClaimsInvestigation>();
            var newClaimsAssigned = new List<ClaimsInvestigation>();

            foreach (var item in applicationDbContext)
            {
                item.ManualNew += 1;
                if (item.ManualNew <= 1)
                {
                    newClaimsAssigned.Add(item);
                }
                claimsAssigned.Add(item);
            }
            if (newClaimsAssigned.Count > 0)
            {
                _context.ClaimsInvestigation.UpdateRange(newClaimsAssigned);
                _context.SaveChanges();
            }
            var response = claimsAssigned?
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        AssignedToAgency = a.AssignedToAgency,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = string.Format(hindiNFO, "{0:c}", a.PolicyDetail.SumAssuredValue),
                        Agent = !string.IsNullOrWhiteSpace(a.CurrentClaimOwner) ?
                        string.Join("", "<span class='badge badge-light'>" + a.CurrentClaimOwner + "</span>") :
                        string.Join("", "<span class='badge badge-light'>" + a.UpdatedBy + "</span>"),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.Name != null ? a.CustomerDetail?.Name : "<span class=\"badge badge-light\"> <i class=\"fas fa-question\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseStatus.Name + "</span>"),
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.ORIGIN.GetEnumDisplayName() + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-light\"> <i class=\"fas fa-question\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                        IsNewAssigned = a.ManualNew <= 1,
                        Ready2Assign = a.IsReady2Assign,
                        BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,
                        AgencyDeclineComment = a.InvestigationCaseSubStatus == withdrawnByCompany ? a.CompanyWithdrawlComment : a.InvestigationCaseSubStatus == withdrawnByAgency ? a.AgencyDeclineComment : a.InvestigationCaseSubStatus == reAssignedStatus ? a.CompanyWithdrawlComment : ""
                    })
                    ?.ToList();

            return Ok(response);
        }
        [HttpGet("GetActive")]
        public IActionResult GetActive()
        {
            IQueryable<ClaimsInvestigation> applicationDbContext = claimsService.GetClaims().Include(a => a.PreviousClaimReports);
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var companyUser = _context.ClientCompanyApplicationUser.Include(u => u.ClientCompany).FirstOrDefault(c => c.Email == userEmail.Value);
            applicationDbContext = applicationDbContext.Where(i => i.ClientCompanyId == companyUser.ClientCompanyId);

            var openStatuses = _context.InvestigationCaseStatus.Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED)).ToList();
            var createdStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                        i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assigned2AssignerStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var withdrawnByCompanyStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var declinedByAgencyStatus = _context.InvestigationCaseSubStatus.FirstOrDefault(
                       i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);
            var claimsSubmitted = new List<ClaimsInvestigation>();
            var openStatusesIds = openStatuses.Select(i => i.InvestigationCaseStatusId).ToList();

            var claims = applicationDbContext.Where(a => openStatusesIds.Contains(a.InvestigationCaseStatusId) &&
            a.ClientCompanyId == companyUser.ClientCompanyId
            && a.InvestigationCaseSubStatusId != createdStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != withdrawnByCompanyStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != declinedByAgencyStatus.InvestigationCaseSubStatusId
            && a.InvestigationCaseSubStatusId != assigned2AssignerStatus.InvestigationCaseSubStatusId
            );
            List<ClaimsInvestigation> newClaims = new List<ClaimsInvestigation>();
            foreach (var claim in claims)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase &&
                c.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                }
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);

                if (userHasClaimLog)
                {
                    claim.ActiveView += 1;
                    if (claim.ActiveView <= 1)
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
            var profilebuilder = new StringBuilder();
            profilebuilder.Append("<i class='fas fa-portrait'></i> Profile Image <span class='badge badge-light'></span>");

            var response = claimsSubmitted
                    .Select(a => new ClaimsInvesgationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "" : a.CustomerDetail.Name,
                        BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "" : a.BeneficiaryDetail.Name,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = string.Format(hindiNFO, "{0:c}", a.PolicyDetail.SumAssuredValue),
                        AssignedToAgency = a.AssignedToAgency,
                        Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                        OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.ClaimType, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail?.DocumentImage != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : NO_USER,
                        Name = a.CustomerDetail?.Name != null ?
                        a.CustomerDetail?.Name : "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                        Policy = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.LineOfBusiness.Name + "</span>"),
                        Status = string.Join("", "ORIGIN of Claim: " + a.ORIGIN.GetEnumDisplayName() + ""),
                        SubStatus = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Ready2Assign = a.IsReady2Assign,
                        ServiceType = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail?.ClaimType.GetEnumDisplayName() + "(" + a.PolicyDetail.InvestigationServiceType.Name + ")</span>"),
                        Service = string.Join("", "<span class='badge badge-light'>" + a.PolicyDetail.InvestigationServiceType.Name + "</span>"),
                        Location = string.Join("", "<span class='badge badge-light'>" + a.InvestigationCaseSubStatus.Name + "</span>"),
                        Created = string.Join("", "<span class='badge badge-light'>" + a.Created.ToString("dd-MM-yyyy") + "</span>"),
                        timePending = a.GetTimePending(),
                        Withdrawable = !a.NotWithdrawable,
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" :
                        a.BeneficiaryDetail.Name,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                        IsNewAssigned = a.ActiveView <= 1
                    })?
                    .ToList();

            return Ok(response);
        }
        private byte[] GetOwner(ClaimsInvestigation a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
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