using System.Globalization;
using System.Security.Claims;

using Google.Api;

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
using static risk.control.system.Helpers.Permissions;

using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

namespace risk.control.system.Controllers.Api.Agency
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/agency/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class VendorInvestigationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<VendorApplicationUser> userManager;
        private static HttpClient httpClient = new();

        public VendorInvestigationController(ApplicationDbContext context,
             IWebHostEnvironment webHostEnvironment,
             UserManager<VendorApplicationUser> userManager)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.userManager = userManager;
        }

        [HttpGet("GetOpen")]
        public async Task<IActionResult> GetOpen()
        {
            var currentUserEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            List<InvestigationTask> claims = null;
            if (vendorUser.Role.ToString() == AppRoles.SUPERVISOR.ToString())
            {
                claims = await _context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
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
                .Where(a=> a.Status == CONSTANTS.CASE_STATUS.INPROGRESS)
                .Where(a=> a.VendorId == vendorUser.VendorId)
                .Where(a => (a.AllocatingSupervisordEmail == currentUserEmail) && 
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ))
                .Where(a => (a.SubmittingSupervisordEmail == currentUserEmail) &&
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)).ToListAsync();
            }
            else
            {
                claims = await _context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
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
                .Where(a=> a.Status == CONSTANTS.CASE_STATUS.INPROGRESS)
                .Where(a => a.VendorId == vendorUser.VendorId &&
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ||
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)).ToListAsync();
            }

            var response = claims?.Select(a => new ClaimsInvestigationResponse
            {
                Id = a.Id,
                AssignedToAgency = a.IsNewSubmittedToAgent,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                Agent = GetOwnerEmail(a),
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(GetOwner(a))),
                CaseWithPerson = IsCaseWithAgent(a),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Company = a.ClientCompany.Name,
                Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = a.CustomerDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.CustomerDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                Name = a.CustomerDetail.Name,
                Policy = $"<span class='badge badge-light'>{a.PolicyDetail.InsuranceType.GetEnumDisplayName()}</span>",
                Status = a.Status,
                ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetSupervisorOpenTimePending(a),
                PolicyNum = a.PolicyDetail.ContractNumber,
                BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) : Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ? "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" : a.BeneficiaryDetail.Name,
                TimeElapsed = GetTimeElapsed(a),
                PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                Distance = a.SelectedAgentDrivingDistance,
                Duration = a.SelectedAgentDrivingDuration
            })?.ToList();

            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = _context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    if (entity.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
                    {
                        entity.IsNewSubmittedToAgent = false;
                    }
                    else if (entity.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
                    {
                        entity.IsNewSubmittedToCompany = false;
                    }

                await _context.SaveChangesAsync(); // mark as viewed
            }
            return Ok(response);
        }

        private double GetTimeElapsed(InvestigationTask a)
        {

            var timeElapsed = DateTime.Now.Subtract(a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT ? a.TaskToAgentTime.Value :
                                                     a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR ? a.SubmittedToAssessorTime.Value :
                                                     a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR ?
                                                     a.EnquiryReplyByAssessorTime.Value : a.Created).TotalSeconds;
            return timeElapsed;
        }
        private string GetSupervisorOpenTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.TaskToAgentTime.Value;
            //1. assigned case to agent
            if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT)
            {
                timeToCompare = a.TaskToAgentTime.GetValueOrDefault();

            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR)
            {
                timeToCompare = a.SubmittedToAssessorTime.GetValueOrDefault();
            }

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }

        private bool IsCaseWithAgent(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

            return (a.SubStatus == allocated2agent);
            
        }
        private string GetOwnerEmail(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;

            if (a.SubStatus == allocated2agent)
            {
                ownerEmail = a.TaskedAgentEmail;
                var agentProfile = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == ownerEmail)?.Email;
                if (agentProfile != null)
                {
                    return agentProfile;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var companyImage = _context.ClientCompany.FirstOrDefault(v => v.ClientCompanyId == a.ClientCompanyId)?.Email;
                if (companyImage == null)
                {
                    return companyImage;
                }
            }
            return "noDataimage";
        }
        private byte[] GetOwner(InvestigationTask a)
        {
            string ownerEmail = string.Empty;
            string ownerDomain = string.Empty;
            string profileImage = string.Empty;
            var allocated2agent = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT;
            var noDataImagefilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-photo.jpg");

            var noDataimage = System.IO.File.ReadAllBytes(noDataImagefilePath);

            if (a.SubStatus == allocated2agent)
            {
                ownerEmail = a.TaskedAgentEmail;
                var agentProfile = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == ownerEmail)?.ProfilePicture;
                if (agentProfile != null)
                {
                    return agentProfile;
                }
            }
            else if (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_ASSESSOR || a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REPLY_TO_ASSESSOR)
            {
                var agentProfile = _context.ClientCompany.FirstOrDefault(u => u.ClientCompanyId == a.ClientCompanyId)?.DocumentImage;
                if (agentProfile != null)
                {
                    return agentProfile;
                }
            }
            return noDataimage;
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

            // Filter claims early and minimize loading
            var claims = await _context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
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
                            (a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR ||
                             a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR)).ToListAsync();
            // Process each claim and update as necessary

            foreach (var claim in claims)
            {

                if (claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING && claim.CustomerDetail != null)
                {
                    // Fetch weather data for HEALTH claims
                    claim.CustomerDetail.AddressLocationInfo = await UpdateWeatherDataAsync(double.Parse(claim.CustomerDetail.Latitude), double.Parse(claim.CustomerDetail.Longitude));
                }
                else if (claim.PolicyDetail.InsuranceType == InsuranceType.CLAIM&& claim.BeneficiaryDetail != null)
                {
                    // Fetch weather data for DEATH claims
                    claim.BeneficiaryDetail.AddressLocationInfo = await UpdateWeatherDataAsync(double.Parse(claim.BeneficiaryDetail.Latitude), double.Parse(claim.BeneficiaryDetail.Longitude));
                }
            }
            
            var response = claims.Select(a => new ClaimsInvestigationAgencyResponse
            {
                Id = a.Id,
                PolicyId = a.PolicyDetail.ContractNumber,
                Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                Company = a.ClientCompany.Name,
                OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                AssignedToAgency = a.AssignedToAgency,
                Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                Status = a.Status,
                ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                Service = a.PolicyDetail.InvestigationServiceType.Name,
                Location = a.SubStatus,
                Created = a.Created.ToString("dd-MM-yyyy"),
                timePending = GetSupervisorNewTimePending(a),
                PolicyNum = GetPolicyNumForAgency(a, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR, CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR),
                BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                    string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                    Applicationsettings.NO_USER,
                BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                    "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                    a.BeneficiaryDetail.Name,
                TimeElapsed = DateTime.Now.Subtract(a.AllocatedToAgencyTime.Value).TotalSeconds,
                IsNewAssigned = a.IsNewAssignedToAgency,
                IsQueryCase = a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR,
                PersonMapAddressUrl = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.CustomerLocationMap : a.BeneficiaryDetail.BeneficiaryLocationMap,
                AddressLocationInfo = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.AddressLocationInfo : a.BeneficiaryDetail.AddressLocationInfo
            }).ToList();
            // Mark claims as viewed
            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = _context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewAssignedToAgency = false;

                await _context.SaveChangesAsync(); // mark as viewed
            }

            return Ok(response);
        }
        private string GetPolicyNumForAgency(InvestigationTask a, string enquiryStatus, string allocatedStatus)
        {
            var claim = a;
            if (claim is not null)
            {
                var isRequested = a.SubStatus == enquiryStatus;
                if (isRequested)
                {
                    return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style\" title=\"ENQUIRY\"></i>");
                }

            }
            return string.Join("", a.PolicyDetail?.ContractNumber + "<i class=\"fa fa-asterisk asterik-style-none\"></i>");
        }
        private string GetSupervisorNewTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.AllocatedToAgencyTime.Value;
            
            var allocated2agency = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR;

            var requested2agency = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR;
            //1. All new case
            if (a.SubStatus  == requested2agency)
            {
                timeToCompare = a.EnquiredByAssessorTime.GetValueOrDefault();
            }

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
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

            // Filter the claims based on the vendor ID and required status
            var claims = _context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
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
                            a.SubStatus == CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR);
            var responseData = await claims.ToListAsync();
            var response = responseData.Select(a =>
                new ClaimsInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(vendorUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                    Document = a.PolicyDetail.DocumentImage != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) : Applicationsettings.NO_POLICY_IMAGE,
                    Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Status = a.SubStatus,
                    ServiceType = a.PolicyDetail?.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    RawStatus = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorReportTimePending(a),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail?.ProfilePicture != null ?
                                           string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                          Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                            "<span class=\"badge badge-danger\"> <i class=\"fas fa-exclamation-triangle\"></i> </span>" :
                            a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.SubmittedToSupervisorTime.Value).TotalSeconds,
                    IsNewAssigned = a.IsNewSubmittedToAgency,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                }).ToList();
            var idsToMarkViewed = response.Where(x => x.IsNewAssigned.GetValueOrDefault()).Select(x => x.Id).ToList();
            if (idsToMarkViewed.Any())
            {
                var entitiesToUpdate = _context.Investigations
                    .Where(x => idsToMarkViewed.Contains(x.Id))
                    .ToList();

                foreach (var entity in entitiesToUpdate)
                    entity.IsNewSubmittedToAgency = false;

                await _context.SaveChangesAsync(); // mark as viewed
            }

            return Ok(response);
        }
        private string GetSupervisorReportTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.SubmittedToSupervisorTime.Value;

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
        [HttpGet("GetCompleted")]
        public async Task<IActionResult> GetCompleted()
        {
            var finishedStatus = CONSTANTS.CASE_STATUS.FINISHED;
            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

            var userEmail = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var currentUserEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(currentUserEmail))
                return Unauthorized("User not authenticated.");

            var agencyUser = await _context.VendorApplicationUser
                .Include(v => v.Country)
                .FirstOrDefaultAsync(c => c.Email == currentUserEmail);

            if (agencyUser == null)
                return NotFound("Agency user not found.");
            var claims = _context.Investigations
                .Include(a => a.ClientCompany)
                .Include(c => c.PolicyDetail)
                .ThenInclude(p => p.InvestigationServiceType)
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
                .Where(a => a.VendorId == agencyUser.VendorId && a.Status == finishedStatus && (a.SubStatus == approvedStatus || a.SubStatus == rejectedStatus));

            if(agencyUser.Role.ToString() == AppRoles.SUPERVISOR.ToString())
            {
                claims = claims
                    .Where(a => a.SubmittedAssessordEmail == currentUserEmail);
            }
            var responseData = await claims.ToListAsync();
            var response = responseData
                .Select(a => new ClaimsInvestigationAgencyResponse
                {
                    Id = a.Id,
                    PolicyId = a.PolicyDetail.ContractNumber,
                    Amount = string.Format(Extensions.GetCultureByCountry(agencyUser.Country.Code.ToUpper()), "{0:C}", a.PolicyDetail.SumAssuredValue),
                    AssignedToAgency = a.AssignedToAgency,
                    Pincode = ClaimsInvestigationExtension.GetPincode(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    PincodeName = ClaimsInvestigationExtension.GetPincodeName(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Company = a.ClientCompany.Name,
                    OwnerDetail = string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.ClientCompany.DocumentImage)),
                    Document = a.PolicyDetail.DocumentImage != null ?
                                string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.PolicyDetail.DocumentImage)) :
                                Applicationsettings.NO_POLICY_IMAGE,
                    Customer = ClaimsInvestigationExtension.GetPersonPhoto(a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING, a.CustomerDetail, a.BeneficiaryDetail),
                    Name = a.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING ? a.CustomerDetail.Name : a.BeneficiaryDetail.Name,
                    Policy = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Status = a.Status,
                    ServiceType = a.PolicyDetail.InsuranceType.GetEnumDisplayName(),
                    Service = a.PolicyDetail.InvestigationServiceType.Name,
                    Location = a.SubStatus,
                    Created = a.Created.ToString("dd-MM-yyyy"),
                    timePending = GetSupervisorCompletedTimePending(a),
                    PolicyNum = a.PolicyDetail.ContractNumber,
                    BeneficiaryPhoto = a.BeneficiaryDetail.ProfilePicture != null ?
                                        string.Format("data:image/*;base64,{0}", Convert.ToBase64String(a.BeneficiaryDetail.ProfilePicture)) :
                                        Applicationsettings.NO_USER,
                    BeneficiaryName = string.IsNullOrWhiteSpace(a.BeneficiaryDetail.Name) ?
                                    "<span class=\"badge badge-danger\"><i class=\"fas fa-exclamation-triangle\"></i></span>" :
                                    a.BeneficiaryDetail.Name,
                    TimeElapsed = DateTime.Now.Subtract(a.ProcessedByAssessorTime.Value).TotalSeconds,
                    PersonMapAddressUrl = string.Format(a.SelectedAgentDrivingMap, "300", "300"),
                    Distance = a.SelectedAgentDrivingDistance,
                    Duration = a.SelectedAgentDrivingDuration
                })
                .ToList();

            return Ok(response);
        }

        private string GetSupervisorCompletedTimePending(InvestigationTask a)
        {
            DateTime timeToCompare = a.ProcessedByAssessorTime.Value;

            if (DateTime.Now.Subtract(timeToCompare).Days >= 1)
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Days} day</span>");

            if (DateTime.Now.Subtract(timeToCompare).Hours < 24 &&
                DateTime.Now.Subtract(timeToCompare).Hours > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Hours} hr </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Hours == 0 && DateTime.Now.Subtract(timeToCompare).Minutes > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Minutes} min </span>");
            }
            if (DateTime.Now.Subtract(timeToCompare).Minutes == 0 && DateTime.Now.Subtract(timeToCompare).Seconds > 0)
            {
                return string.Join("", $"<span class='badge badge-light'>{DateTime.Now.Subtract(timeToCompare).Seconds} sec </span>");
            }
            return string.Join("", "<span class='badge badge-light'>now</span>");
        }
    }
}