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
using System.Linq.Expressions;
using System.Linq;

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
        private readonly ICreatorService creatorService;
        private readonly IClaimsService claimsService;

        public CreatorController(ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            ICreatorService creatorService,
            IClaimsService claimsService)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.creatorService = creatorService;
            this.claimsService = claimsService;
        }
        
        [Authorize(Roles = $"{CREATOR.DISPLAY_NAME}")]
        [HttpGet("GetAuto")]
        public async Task<IActionResult> GetAuto(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            
            var response = await creatorService.GetAuto(currentUserEmail,draw,start,length, search, caseType, orderColumn, orderDir);

            return Ok(response);
            
        }

        private static string GetCreatorAutoTimePending(ClaimsInvestigation a, bool assigned = false)
        {
            if (DateTime.Now.Subtract(a.Updated.Value).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Hurry up, {DateTime.Now.Subtract(a.Updated.Value).Days} days since created!\"></i>");

            else if (DateTime.Now.Subtract(a.Updated.Value).Days >= 3 || DateTime.Now.Subtract(a.Updated.Value).Days >= a.CreatorSla)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span><i data-toggle='tooltip' class=\"fa fa-asterisk asterik-style\" title=\"Caution : {DateTime.Now.Subtract(a.Updated.Value).Days} day since created.\"></i>");
            if (DateTime.Now.Subtract(a.Updated.Value).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Days} day</span>");

            if (DateTime.Now.Subtract(a.Updated.Value).Hours < 24 &&
                DateTime.Now.Subtract(a.Updated.Value).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.Value).Hours == 0 && DateTime.Now.Subtract(a.Updated.Value).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(a.Updated.Value).Minutes == 0 && DateTime.Now.Subtract(a.Updated.Value).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(a.Updated.Value).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
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
        public async Task<IActionResult> GetActive(int draw, int start, int length, string search = "", string caseType = "", int orderColumn = 0, string orderDir = "asc")
        {
            var userEmail = HttpContext.User.Identity.Name;

            var response = await creatorService.GetActive(userEmail, draw, start, length, search, caseType, orderColumn, orderDir);

            return Ok(response);
        }

        [HttpGet("GetFilesData")]
        public async Task<IActionResult> GetFilesData()
        {
            var userEmail = HttpContext.User.Identity.Name;

            var companyUser = _context.ClientCompanyApplicationUser.Include(c=>c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);
            
            var totalReadyToAssign = await creatorService.GetAutoCount(userEmail);
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            var files = await _context.FilesOnFileSystem.Where(f => f.CompanyId == companyUser.ClientCompanyId && ((f.UploadedBy == userEmail && !f.Deleted) || isManager)).ToListAsync();
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
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager
            }).ToList();

            return Ok(new { data = result, maxAssignReadyAllowed = maxAssignReadyAllowedByCompany >= totalReadyToAssign });
        }

        [HttpGet("GetFileById/{uploadId}")]
        public async Task<IActionResult> GetFileById(int uploadId)
        {
            var userEmail = HttpContext.User.Identity.Name;
            var companyUser = _context.ClientCompanyApplicationUser.Include(c=>c.ClientCompany).FirstOrDefault(u => u.Email == userEmail);
            var file =  await _context.FilesOnFileSystem.FirstOrDefaultAsync(f => f.Id == uploadId && f.CompanyId == companyUser.ClientCompanyId && f.UploadedBy == userEmail && !f.Deleted);
            if (file == null)
            {
                return NotFound(new { success = false, message = "File not found." });
            }
            var totalReadyToAssign = await creatorService.GetAutoCount(userEmail);
            var totalForAssign = totalReadyToAssign + file.ClaimsId?.Count;
            var maxAssignReadyAllowedByCompany = companyUser.ClientCompany.TotalToAssignMaxAllowed;

            var isManager = HttpContext.User.IsInRole(MANAGER.DISPLAY_NAME);
            var result =  new
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
                Icon = file.Icon, // or use some other status representation
                IsManager = isManager
            };

            return Ok(new { data = result, maxAssignReadyAllowed = maxAssignReadyAllowedByCompany >= totalForAssign });
        }

        [HttpGet("GetPendingAllocations")]
        public async Task<IActionResult> GetPendingAllocations()
        {
            var userEmail = HttpContext.User.Identity.Name;
            var pendingCount = await _context.ClaimsInvestigation.CountAsync(c=>c.UpdatedBy == userEmail && c.STATUS == ALLOCATION_STATUS.PENDING);
            return Ok(new { count = pendingCount });
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