using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.Applicationsettings;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/agency/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class SupervisorController : ControllerBase
    {
        private const string CLAIM = "claims";
        private const string UNDERWRITING = "underwriting";
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<VendorApplicationUser> userManager;
        private static HttpClient httpClient = new();

        public SupervisorController(ApplicationDbContext context,
             IWebHostEnvironment webHostEnvironment,
             UserManager<VendorApplicationUser> userManager)
        {
            hindiNFO.CurrencySymbol = string.Empty;
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.userManager = userManager;
        }

        [HttpGet("GetOpen")]
        public async Task<IActionResult> GetOpen()
        {
            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("User not authenticated.");

            var vendorUser = await _context.VendorApplicationUser
                .Include(c => c.Country)
                .Include(u => u.Vendor)
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (vendorUser == null)
                return NotFound("Vendor user not found.");

            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            if (vendorUser != null)
            {
                applicationDbContext = applicationDbContext.Where(i => i.VendorId == vendorUser.VendorId);
            }

            var userRole = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            // Fetch the relevant substatus IDs in one query
            var openSubstatusesForSupervisor = await _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR
                }.Contains(i.Name.ToUpper()))
                .Select(s => s.InvestigationCaseSubStatusId)
                .ToListAsync();

            // Fetch status objects in one query
            var statusList = await _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR
                }.Contains(i.Name.ToUpper()))
                .ToListAsync();

            var allocatedToVendorStatus = statusList.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR);
            var assignedToAgentStatus = statusList.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT);
            var replyStatus = statusList.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR);
            var submittedToAssessorStatus = statusList.FirstOrDefault(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            if (string.IsNullOrEmpty(userRole))
                return Unauthorized("User role not found.");

            IList<ClaimsInvestigation> claimsSubmitted = null;

            if (userRole.Contains(AppRoles.AGENCY_ADMIN.ToString()))
            {
                // Filtering for Agency Admin role
                applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                    (a.InvestigationCaseSubStatus == assignedToAgentStatus ||
                     a.InvestigationCaseSubStatus == replyStatus ||
                     a.InvestigationCaseSubStatus == submittedToAssessorStatus));
            }
            else if (userRole.Contains(AppRoles.SUPERVISOR.ToString()))
            {
                // Filtering for Supervisor role
                applicationDbContext = applicationDbContext.Where(a => openSubstatusesForSupervisor.Contains(a.InvestigationCaseSubStatusId) &&
                    (a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == replyStatus ||
                     a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == assignedToAgentStatus ||
                     a.UserEmailActioned == vendorUser.Email && a.InvestigationCaseSubStatus == submittedToAssessorStatus));
            }

            claimsSubmitted = await applicationDbContext.ToListAsync();

            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;
            var response = claimsSubmitted?.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.ClaimsInvestigationId,
                AssignedToAgency = a.AssignedToAgency,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                Agent = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo) ? a.UserEmailActionedTo : a.UserRoleActionedTo,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                CaseWithPerson = !string.IsNullOrWhiteSpace(a.UserEmailActionedTo),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                Company = a.ClientCompany.Name,
                Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail.Name,
                Policy = $"<span class='badge badge-light'>{a.PolicyDetail.LineOfBusiness.Name}</span>",
                Status = a.InvestigationCaseStatus.Name,
                ServiceType = a.PolicyDetail.LineOfBusiness.Name,
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.InvestigationCaseSubStatus.Name,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = a.GetSupervisorTimePending(false, a.InvestigationCaseSubStatus == assignedToAgentStatus, false, a.InvestigationCaseSubStatus == submittedToAssessorStatus, a.InvestigationCaseSubStatus == replyStatus),
                PolicyNum = a.PolicyDetail.ContractNumber,
                BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.InvestigationCaseSubStatus == assignedToAgentStatus ? a.TaskToAgentTime.Value :
                                                     a.InvestigationCaseSubStatus == submittedToAssessorStatus ? a.SubmittedToAssessorTime.Value :
                                                     a.InvestigationCaseSubStatus == replyStatus ? a.EnquiryReplyByAssessorTime.Value : a.Created).TotalSeconds,
                PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration
            }).ToList();

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

        [HttpGet("GetNew")]
        public async Task<IActionResult> GetNew()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (vendorUser == null)
                return NotFound("Vendor not found.");

            // Fetch relevant status in one query for efficiency
            var statuses = await _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT
                }.Contains(i.Name.ToUpper()))
                .ToDictionaryAsync(i => i.Name.ToUpper(), i => i.InvestigationCaseSubStatusId);

            if (statuses.Count < 3)
                return BadRequest("Missing required case statuses.");

            // Filter claims early and minimize loading
            var claims = await _context.ClaimsInvestigation
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Include(a => a.PolicyDetail)
                .ThenInclude(a => a.LineOfBusiness)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Where(a => a.VendorId == vendorUser.VendorId &&
                            (a.InvestigationCaseSubStatusId == statuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR] ||
                             a.InvestigationCaseSubStatusId == statuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR]))
                .ToListAsync();

            var newAllocateClaims = new List<ClaimsInvestigation>();
            var claimLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == CLAIM).LineOfBusinessId;
            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            // Process each claim and update as necessary
            foreach (var claim in claims)
            {
                claim.AllocateView += 1;
                if (claim.AllocateView <= 1)
                {
                    newAllocateClaims.Add(claim);
                }

                if (claim.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness && claim.CustomerDetail != null)
                {
                    // Fetch weather data for HEALTH claims
                    claim.CustomerDetail.AddressLocationInfo = await UpdateWeatherDataAsync(double.Parse(claim.CustomerDetail.Latitude), double.Parse(claim.CustomerDetail.Longitude));
                }
                else if (claim.PolicyDetail.LineOfBusinessId == claimLineOfBusiness && claim.BeneficiaryDetail != null)
                {
                    // Fetch weather data for DEATH claims
                    claim.BeneficiaryDetail.AddressLocationInfo = await UpdateWeatherDataAsync(double.Parse(claim.BeneficiaryDetail.Latitude), double.Parse(claim.BeneficiaryDetail.Longitude));
                }
            }

            // Update new claims
            if (newAllocateClaims.Any())
            {
                _context.ClaimsInvestigation.UpdateRange(newAllocateClaims);
                await _context.SaveChangesAsync();
            }

            var response = claims.Select(a => new ClaimsInvestigationAgencyResponse
            {
                Id = a.ClaimsInvestigationId,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                Company = a.ClientCompany.Name,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                AssignedToAgency = a.AssignedToAgency,
                Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                Name = a.PolicyDetail.LineOfBusinessId ==underWritingLineOfBusiness ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                Policy = a.PolicyDetail?.LineOfBusiness.Name,
                Status = a.InvestigationCaseStatus.Name,
                ServiceType = a.PolicyDetail?.LineOfBusiness.Name,
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.InvestigationCaseSubStatus.Name,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = a.GetSupervisorTimePending(!a.IsQueryCase, false, false, false, a.IsQueryCase),
                PolicyNum = a.GetPolicyNumForAgency(statuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR]),
                BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                    Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                    a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds,
                IsNewAssigned = a.AllocateView <= 1,
                IsQueryCase = a.InvestigationCaseSubStatusId == statuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR],
                PersonMapAddressUrl = a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap,
                AddressLocationInfo = a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo
            }).ToList();

            return Ok(response);
        }

        private async Task<string> UpdateWeatherDataAsync(double latitude, double longitude)
        {
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
            var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);

            string weatherCustomData = $"Temperature: {weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                                       $"\r\nWindspeed: {weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                                       $"\r\nElevation(sea level): {weatherData.elevation} metres";

            return weatherCustomData;
        }

        [HttpGet("GetReport")]
        public async Task<IActionResult> GetReport()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            // Fetch the vendor user along with the related Vendor and Country info in one query
            var vendorUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .Include(u => u.Vendor)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (vendorUser == null)
                return NotFound("Vendor not found.");

            // Fetch the required statuses in one query
            var statuses = await _context.InvestigationCaseSubStatus
                .Where(i => new[]
                {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR
                }.Contains(i.Name.ToUpper()))
                .ToDictionaryAsync(i => i.Name.ToUpper(), i => i.InvestigationCaseSubStatusId);

            if (statuses.Count < 3)
                return BadRequest("Missing required case statuses.");

            // Filter the claims based on the vendor ID and required status
            var claims = await _context.ClaimsInvestigation
                .Include(c => c.ClientCompany)
                .Include(c => c.InvestigationCaseStatus)
                .Include(c => c.InvestigationCaseSubStatus)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.CustomerDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.PinCode)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.District)
                .Include(c => c.BeneficiaryDetail)
                .ThenInclude(p => p.State)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.LineOfBusiness)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
                .Where(i => i.VendorId == vendorUser.VendorId &&
                            string.IsNullOrEmpty(i.UserEmailActionedTo) &&
                            i.UserRoleActionedTo == vendorUser.Vendor.Email &&
                            i.InvestigationCaseSubStatusId == statuses[CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR])
                .ToListAsync();
            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            var claimsSubmitted = claims.Select(a =>
            {
                a.VerifyView += 1;
                bool isNewAssigned = a.VerifyView <= 1;

                if (isNewAssigned)
                {
                    // Mark for later saving
                    _context.ClaimsInvestigation.Update(a);
                }

                return new ClaimsInvestigationAgencyResponse
                {
                    Id = a.ClaimsInvestigationId,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                    Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    Name = a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail?.LineOfBusiness.Name,
                    Status = a.InvestigationCaseStatus.Name,
                    ServiceType = a.PolicyDetail?.LineOfBusiness.Name,
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.InvestigationCaseSubStatus.Name,
                    RawStatus = a.InvestigationCaseSubStatus.Name,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetSupervisorTimePending(false, false, true, false),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                       string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                      Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                        "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                        a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds,
                    IsNewAssigned = isNewAssigned,
                    PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                };
            }).ToList();

            // Save changes if any new claims were marked
            if (claimsSubmitted.Any(c => c.IsNewAssigned.HasValue && c.IsNewAssigned.Value))
            {
                await _context.SaveChangesAsync();
            }

            return Ok(claimsSubmitted);
        }

        [HttpGet("GetCompleted")]
        public async Task<IActionResult> GetCompleted()
        {
            var finishedStatus = await _context.InvestigationCaseStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.FINISHED);
            var inprogressStatus = await _context.InvestigationCaseStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.INPROGRESS);
            var approvedStatus = await _context.InvestigationCaseSubStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR);
            var rejectedStatus = await _context.InvestigationCaseSubStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR);
            var reassignedStatus = await _context.InvestigationCaseSubStatus
                .FirstOrDefaultAsync(i => i.Name.ToUpper() == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REASSIGNED_TO_ASSIGNER);
            var submittedToAssessorStatus = await _context.InvestigationCaseSubStatus
                .FirstOrDefaultAsync(i => i.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR);

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(currentUserEmail))
                return Unauthorized("User not authenticated.");

            var agencyUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (agencyUser == null)
                return NotFound("Agency user not found.");

            IQueryable<ClaimsInvestigation> applicationDbContext = GetClaims();

            // Fetch the review cases
            var reviewCases = await _context.InvestigationTransaction
                .Where(i => i.IsReviewCase &&
                            i.InvestigationCaseStatusId == inprogressStatus.InvestigationCaseStatusId &&
                            i.InvestigationCaseSubStatusId == reassignedStatus.InvestigationCaseSubStatusId &&
                            i.UserEmailActionedTo == string.Empty)
                .Distinct()
                .ToListAsync();

            List<ClaimsInvestigation> claimsSubmitted = new List<ClaimsInvestigation>();

            if (agencyUser.IsVendorAdmin)
            {
                // Logic for vendor admin role
                var reviewClaimIds = reviewCases.Select(r => r.ClaimsInvestigationId).ToList();
                foreach (var claim in applicationDbContext)
                {
                    var previousReport = await _context.PreviousClaimReport
                        .AnyAsync(r => r.VendorId == claim.VendorId && claim.ClaimsInvestigationId == r.ClaimsInvestigationId);

                    if ((claim.InvestigationCaseStatusId == finishedStatus.InvestigationCaseStatusId &&
                         claim.VendorId == agencyUser.VendorId &&
                         claim.InvestigationCaseSubStatusId == approvedStatus.InvestigationCaseSubStatusId ||
                         claim.InvestigationCaseSubStatusId == rejectedStatus.InvestigationCaseSubStatusId) ||
                        (reviewClaimIds.Contains(claim.ClaimsInvestigationId)) && claim.ReviewCount == 1 && claim.IsReviewCase && previousReport)
                    {
                        claimsSubmitted.Add(claim);
                    }
                }
            }
            else
            {
                // Logic for other roles
                var userAttendedClaims = await _context.InvestigationTransaction
                    .Where(t => t.UserEmailActioned == agencyUser.Email &&
                                t.InvestigationCaseSubStatusId == submittedToAssessorStatus.InvestigationCaseSubStatusId)
                    .Select(c => c.ClaimsInvestigationId)
                    .Distinct()
                    .ToListAsync();

                foreach (var claim in applicationDbContext)
                {
                    var previousReport = await _context.PreviousClaimReport
                        .AnyAsync(r => r.ClaimsInvestigationId == claim.ClaimsInvestigationId);

                    var isReview = reviewCases.Any(i => i.IsReviewCase &&
                                                        claim.ReviewCount == 1 &&
                                                        i.ClaimsInvestigationId == claim.ClaimsInvestigationId &&
                                                        i.InvestigationCaseSubStatusId == reassignedStatus.InvestigationCaseSubStatusId &&
                                                        i.UserEmailActionedTo == string.Empty &&
                                                        i.UserRoleActionedTo == $"{claim.ClientCompany.Email}");

                    if ((claim.InvestigationCaseStatus.Name == CONSTANTS.CASE_STATUS.FINISHED &&
                         claim.VendorId == agencyUser.VendorId &&
                         claim.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR) ||
                        (claim.InvestigationCaseSubStatus.Name == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR) ||
                        (isReview && previousReport))
                    {
                        if (userAttendedClaims.Contains(claim.ClaimsInvestigationId))
                        {
                            claimsSubmitted.Add(claim);
                        }
                    }
                }
            }
            var underWritingLineOfBusiness = _context.LineOfBusiness.FirstOrDefault(l => l.Name.ToLower() == UNDERWRITING).LineOfBusinessId;

            var response = claimsSubmitted
                .Select(a => new ClaimsInvestigationAgencyResponse
                {
                    Id = a.ClaimsInvestigationId,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(agencyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.LineOfBusinessId== underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                    Document = a.PolicyDetail.DocumentImage != null ?
                                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) :
                                Applicationsettings.NO_POLICY_IMAGE,
                    Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness, a.CustomerDetail, a.BeneficiaryDetail),
                    Name = a.PolicyDetail.LineOfBusinessId == underWritingLineOfBusiness ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail?.LineOfBusiness.Name,
                    Status = a.InvestigationCaseStatus.Name,
                    ServiceType = a.PolicyDetail?.LineOfBusiness.Name,
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.InvestigationCaseSubStatus.Name,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = a.GetSupervisorTimePending(false, false, false, true),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                        Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                                    "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" :
                                    a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                    PersonMapAddressUrl = a.SelectedAgentDrivingMap,
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                })
                .ToList();

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