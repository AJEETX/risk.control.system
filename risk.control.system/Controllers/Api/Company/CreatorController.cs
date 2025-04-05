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
using Google.Api;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{CREATOR.DISPLAY_NAME},{MANAGER.DISPLAY_NAME}")]
    public class CreatorController : ControllerBase
    {
        private const string UNDERWRITING = "underwriting";
        private const string CLAIMS = "claims";
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IManageCaseService manageCaseService;
        private readonly IClaimsService claimsService;

        public CreatorController(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            IManageCaseService manageCaseService,
            IClaimsService claimsService)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.manageCaseService = manageCaseService;
            this.claimsService = claimsService;
        }
        
        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetAuto")]
        public IActionResult GetAuto()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefault(c => c.Email == currentUserEmail);

            if (companyUser == null)
                return NotFound("User not found.");

            // Fetching all relevant substatuses in a single query for efficiency
            var subStatuses = _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY
                }.Contains(i.Name.ToUpper()))
                .ToDictionary(i => i.Name.ToUpper(), i => i.InvestigationCaseSubStatusId);

            if (subStatuses.Count < 5)
                return BadRequest("Missing required sub-statuses.");

            var claims = claimsService.GetClaims()
                .Where(a =>
                    a.ClientCompanyId == companyUser.ClientCompanyId &&
                    (
                        (a.UserEmailActioned == companyUser.Email && a.UserEmailActionedTo == companyUser.Email && a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR]) ||
                        (a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY] && a.UserEmailActionedTo == string.Empty && a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}") ||
                        (a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY] && a.UserEmailActionedTo == companyUser.Email && a.UserEmailActioned == companyUser.Email && a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}") ||
                        (a.UserEmailActioned == companyUser.Email && a.UserEmailActionedTo == companyUser.Email && a.InvestigationCaseSubStatusId == subStatuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER])
                    )
                )
                .ToList();

            var claimsAssigned = new List<ClaimsInvestigation>();
            var newClaimsAssigned = new List<ClaimsInvestigation>();

            // Process claims and update AutoNew
            foreach (var item in claims)
            {
                item.AutoNew += 1;
                if (item.AutoNew <= 1)
                {
                    newClaimsAssigned.Add(item);
                }
                claimsAssigned.Add(item);
            }

            if (newClaimsAssigned.Any())
            {
                _context.ClaimsInvestigation.UpdateRange(newClaimsAssigned);
                _context.SaveChanges();
            }
            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            // Prepare response
            var response = claimsAssigned
                .Select(a => new ClaimsInvestigationResponse
                {
                    Id = a.ClaimsInvestigationId,
                    Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    PolicyId = a.PolicyDetail.ContractNumber,
                    AssignedToAgency = a.AssignedToAgency,
                    AutoAllocated = a.AutoAllocated,
                    Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                    Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-light\">customer name</span>",
                    Policy = a.PolicyDetail?.LineOfBusiness.Name,
                    Status = a.STATUS.GetEnumDisplayName(),
                    SubStatus = a.InvestigationCaseSubStatus.Name,
                    Ready2Assign = a.IsReady2Assign,
                    ServiceType = $"{a.PolicyDetail?.LineOfBusiness.Name} ( {a.PolicyDetail.InvestigationServiceType.Name})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.ORIGIN.GetEnumDisplayName(),
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetCreatorTimePending(),
                    Withdrawable = !a.NotWithdrawable,
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-light\">beneficiary name</span>" : a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.Updated.GetValueOrDefault()).TotalSeconds,
                    IsNewAssigned = a.AutoNew <= 1,
                    BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                    CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,
                    PersonMapAddressUrl = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail) != "..." ?
                        a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap : Applicationsettings.NO_MAP
                })
                .ToList();

            return Ok(response);
        }

        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("RefreshData")]
        public async Task<IActionResult> RefreshData(string id)
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser
                .Include(c => c.Country)
                .FirstOrDefault(c => c.Email == currentUserEmail);

            if (companyUser == null)
                return NotFound("User not found.");

            var claim = claimsService.GetClaims()
                .FirstOrDefault(a => a.ClaimsInvestigationId == id);
            if(DateTime.Now.Subtract(claim.Created).TotalSeconds > 20)
            {
                claim.STATUS = ALLOCATION_STATUS.ERROR;
            }
            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            // Prepare response
            var response =  new ClaimsInvestigationResponse
                {
                    Id = claim.ClaimsInvestigationId,
                    Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:C}", claim.PolicyDetail.SumAssuredValue),
                    PolicyId = claim.PolicyDetail.ContractNumber,
                    AssignedToAgency = claim.AssignedToAgency,
                    AutoAllocated = claim.AutoAllocated,
                    Agent = !string.IsNullOrWhiteSpace(claim.UserEmailActionedTo) ? claim.UserEmailActionedTo : claim.UserRoleActionedTo,
                    Pincode = ClaimsInvestigationExtension.GetPincode(claim.PolicyDetail.LineOfBusinessId== underWritingLineOfBusiness, claim.CustomerDetail, claim.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, claim.CustomerDetail, claim.BeneficiaryDetail),
                    Document = claim.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.PolicyDetail?.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer = claim.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                    Name = claim.CustomerDetail?.Name ?? "<span class=\"badge badge-light\">customer name</span>",
                    Policy = claim.PolicyDetail?.LineOfBusiness.Name,
                    Status = claim.STATUS.GetEnumDisplayName(),
                    SubStatus = claim.InvestigationCaseSubStatus.Name,
                    Ready2Assign = claim.IsReady2Assign,
                    ServiceType = $"{claim.PolicyDetail?.LineOfBusiness.Name} ( {claim.PolicyDetail.InvestigationServiceType.Name})",
                    Service = claim.PolicyDetail.InvestigationServiceType.Name,
                    Location = claim.ORIGIN.GetEnumDisplayName(),
                    Created = claim.Created.ToString("dd-MM-yyyy"),
                    timePending = claim.GetCreatorTimePending(),
                    Withdrawable = !claim.NotWithdrawable,
                    PolicyNum = claim.GetPolicyNum(),
                    BeneficiaryPhoto = claim.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(claim.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(claim.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-light\">beneficiary name</span>" : claim.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(claim.Updated.GetValueOrDefault()).TotalSeconds,
                    IsNewAssigned = claim.AutoNew <= 1,
                    BeneficiaryFullName = string.IsNullOrWhiteSpace(claim.BeneficiaryDetail?.Name) ? "?" : claim.BeneficiaryDetail.Name,
                    CustomerFullName = string.IsNullOrWhiteSpace(claim.CustomerDetail?.Name) ? "?" : claim.CustomerDetail.Name,
                    PersonMapAddressUrl = ClaimsInvestigationExtension.GetPincodeName(claim.PolicyDetail.LineOfBusinessId== underWritingLineOfBusiness, claim.CustomerDetail, claim.BeneficiaryDetail) != "..." ?
                        claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? claim.CustomerDetail.CustomerLocationMap : claim.BeneficiaryDetail.BeneficiaryLocationMap : null
                };

            return Ok(response);
        }

        
        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        public IActionResult GetManual()
        {
            IQueryable<ClaimsInvestigation> claims = claimsService.GetClaims();

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

            var companyUser = _context.ClientCompanyApplicationUser.Include(c => c.Country).Include(c => c.ClientCompany).FirstOrDefault(c => c.Email == userEmail.Value);

            claims = claims.Where(a => a.ClientCompanyId == companyUser.ClientCompanyId &&
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
                        (a.CREATEDBY == CREATEDBY.MANUAL && a.UserEmailActioned == companyUser.Email &&
                         a.UserEmailActionedTo == companyUser.Email &&
                         a.InvestigationCaseSubStatusId == createdStatus.InvestigationCaseSubStatusId) ||
                (a.IsReviewCase && a.InvestigationCaseSubStatusId == reAssignedStatus.InvestigationCaseSubStatusId &&
                a.UserEmailActionedTo == string.Empty &&
                a.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")
                );

            var claimsAssigned = new List<ClaimsInvestigation>();
            var newClaimsAssigned = new List<ClaimsInvestigation>();

            foreach (var item in claims)
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
            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            var response = claimsAssigned?
                    .Select(a => new ClaimsInvestigationResponse
                    {
                        Id = a.ClaimsInvestigationId,
                        AutoAllocated = a.AutoAllocated,
                        AssignedToAgency = a.AssignedToAgency,
                        PolicyId = a.PolicyDetail.ContractNumber,
                        Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                        Agent = !string.IsNullOrWhiteSpace(a.CurrentClaimOwner) ? a.CurrentClaimOwner : a.UpdatedBy,
                        Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                        PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                        Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                        Customer = a.CustomerDetail?.ProfilePicture != null ?
                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : Applicationsettings.NO_USER,
                        Name = a.CustomerDetail?.Name != null ? a.CustomerDetail?.Name : "<span class=\"badge badge-light\">customer name</span>",
                        Policy = a.PolicyDetail?.LineOfBusiness.Name,
                        Status = a.InvestigationCaseStatus.Name,
                        ServiceType = a.PolicyDetail?.LineOfBusiness.Name,
                        Service = a.PolicyDetail.InvestigationServiceType.Name,
                        Location = a.ORIGIN.GetEnumDisplayName(),
                        Created = a.Created.ToString("dd-MM-yyyy"),
                        timePending = a.GetCreatorTimePending(false, a.IsReviewCase),
                        PolicyNum = a.GetPolicyNum(),
                        BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                        BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ?
                        "<span class=\"badge badge-light\">beneficiary name</span>" :
                        a.BeneficiaryDetail.Name,
                        TimeElapsed = DateTime.Now.Subtract(a.Created).TotalSeconds,
                        IsNewAssigned = a.ManualNew <= 1,
                        Ready2Assign = a.IsReady2Assign,
                        BeneficiaryFullName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "?" : a.BeneficiaryDetail.Name,
                        CustomerFullName = string.IsNullOrWhiteSpace(a.CustomerDetail?.Name) ? "?" : a.CustomerDetail.Name,
                        AgencyDeclineComment = a.InvestigationCaseSubStatus == withdrawnByCompany ? 
                        a.CompanyWithdrawlComment : a.InvestigationCaseSubStatus == withdrawnByAgency ? 
                        a.AgencyDeclineComment : a.InvestigationCaseSubStatus == reAssignedStatus ? a.CompanyWithdrawlComment : "",
                        PersonMapAddressUrl = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail) != "..." ?
                        a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap : null
                    })
                    ?.ToList();

            return Ok(response);
        }
        
        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetActive")]
        public IActionResult GetActive()
        {
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser
                .Include(u => u.Country)
                .Include(u => u.ClientCompany)
                .FirstOrDefault(c => c.Email == userEmail);

            if (companyUser == null) return NotFound("User not found.");

            var openStatuses = _context.InvestigationCaseStatus
                .Where(i => !i.Name.Contains(CONSTANTS.CASE_STATUS.FINISHED))
                .Select(i => i.InvestigationCaseStatusId)
                .ToList();

            var createdStatus = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR);
            var assigned2AssignerStatus = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER);
            var withdrawnByCompanyStatus = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY);
            var declinedByAgencyStatus = _context.InvestigationCaseSubStatus
                .FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY);

            if (createdStatus == null || assigned2AssignerStatus == null || withdrawnByCompanyStatus == null || declinedByAgencyStatus == null)
                return NotFound("SubStatus not found.");

            var claims = _context.ClaimsInvestigation
                .Where(a => openStatuses.Contains(a.InvestigationCaseStatusId) &&
                            a.ClientCompanyId == companyUser.ClientCompanyId &&
                            !new[] { createdStatus.InvestigationCaseSubStatusId, withdrawnByCompanyStatus.InvestigationCaseSubStatusId,
                            declinedByAgencyStatus.InvestigationCaseSubStatusId, assigned2AssignerStatus.InvestigationCaseSubStatusId }
                            .Contains(a.InvestigationCaseSubStatusId))
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p=>p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p=>p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p=>p.LineOfBusiness)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .ToList();

            var claimsSubmitted = new List<ClaimsInvestigation>();
            var newClaims = new List<ClaimsInvestigation>();

            foreach (var claim in claims)
            {
                var userHasReviewClaimLogs = _context.InvestigationTransaction.Where(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.IsReviewCase && c.UserRoleActionedTo == $"{companyUser.ClientCompany.Email}")?.ToList();

                int? reviewLogCount = 0;
                if (userHasReviewClaimLogs != null && userHasReviewClaimLogs.Count > 0)
                {
                    reviewLogCount = userHasReviewClaimLogs.OrderByDescending(o => o.HopCount).First().HopCount;
                }
                var userHasClaimLog = _context.InvestigationTransaction.Any(c => c.ClaimsInvestigationId == claim.ClaimsInvestigationId && c.UserEmailActioned == companyUser.Email && c.HopCount >= reviewLogCount);

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

            if (newClaims.Any())
            {
                _context.ClaimsInvestigation.UpdateRange(newClaims);
                _context.SaveChanges();
            }

            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            var response = claimsSubmitted
                .Select(a => new ClaimsInvestigationResponse
                {
                    Id = a.ClaimsInvestigationId,
                    AutoAllocated = a.AutoAllocated,
                    CustomerFullName = a.CustomerDetail?.Name ?? "",
                    BeneficiaryFullName = a.BeneficiaryDetail?.Name ?? "",
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(companyUser.Country.Code.ToUpper()), "{0:c}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                    CaseWithPerson = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo),
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    Document = a.PolicyDetail?.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail?.DocumentImage)) : NO_POLICY_IMAGE,
                    Customer = a.CustomerDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail?.ProfilePicture)) : NO_USER,
                    Name = a.CustomerDetail?.Name ?? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>",
                    Policy = a.PolicyDetail?.LineOfBusiness.Name,
                    Status = a.ORIGIN.GetEnumDisplayName(),
                    SubStatus = a.InvestigationCaseSubStatus.Name,
                    Ready2Assign = a.IsReady2Assign,
                    ServiceType = $"{a.PolicyDetail.LineOfBusiness.Name} ({a.PolicyDetail.InvestigationServiceType.Name})",
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.InvestigationCaseSubStatus.Name,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetCreatorTimePending(true),
                    Withdrawable = !a.NotWithdrawable,
                    PolicyNum = a.GetPolicyNum(),
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail?.Name) ? "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\" ></i>  </span>" : a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.GetValueOrDefault()).TotalSeconds,
                    IsNewAssigned = a.ActiveView <= 1,
                    PersonMapAddressUrl = a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap
                })
                .ToList();

            return Ok(response);
        }

        [HttpGet("GetFilesData")]
        public async Task<IActionResult> GetFilesData()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == userEmail);
            var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);

            var files = await _context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId && (f.UploadedBy == userEmail || isManager)).ToListAsync();
            var result = files.OrderBy(o=>o.CreatedOn).Select(file => new
            {
                file.Id,
                SequenceNumber = isManager ? file.CompanySequenceNumber : file.UserSequenceNumber,
                file.Name,
                file.Description,
                file.FileType,
                CreatedOn = file.CreatedOn.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm:ss"),
                file.UploadedBy,
                Status = file.Status,
                file.Message,
                Icon = file.Icon // or use some other status representation
            }).ToList();

            return Ok(new { data = result });
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