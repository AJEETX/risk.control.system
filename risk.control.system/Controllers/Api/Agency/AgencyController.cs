using System.Collections.Concurrent;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyController : ControllerBase
    {
        private readonly string noUserImagefilePath = string.Empty;
        private readonly string noDataImagefilePath = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly IDashboardService dashboardService;
        private readonly IFeatureManager featureManager;
        private readonly IUserService userService;
        private readonly ICustomApiCLient customApiCLient;

        public AgencyController(ApplicationDbContext context,
            IFeatureManager featureManager,
            IUserService userService,
            ICustomApiCLient customApiCLient,
            IDashboardService dashboardService)
        {
            this.dashboardService = dashboardService;
            this.featureManager = featureManager;
            this.userService = userService;
            this.customApiCLient = customApiCLient;
            _context = context;
            noUserImagefilePath = "/img/no-user.png";
            noDataImagefilePath = "/img/no-image.png";
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var vendorUsers = _context.VendorApplicationUser
                 .Include(u => u.Country)
                 .Include(u => u.State)
                 .Include(u => u.District)
                 .Include(u => u.PinCode)
                 .Where(c => c.VendorId == vendorUser.VendorId && !c.Deleted);

            var users = vendorUsers?
                .Where(u => !u.Deleted && u.Email != userEmail);

            var allUsers = users?
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName);
            var result = allUsers?.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = "<a href=''>" + u.Email + "</a>",
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Photo = u.ProfilePicture == null ? noUserImagefilePath : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(u.ProfilePicture)),
                    Active = u.Active,
                    Addressline = u.Addressline + ", " + u.District.Name,
                    District = u.District.Name,
                    State = u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Pincode = u.PinCode.Code,
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            users?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return Ok(result);
        }

        [HttpGet("AllAgencies")]
        public async Task<IActionResult> AllAgencies()
        {
            var allAgencies = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .Where(v => !v.Deleted);

            var agencies = allAgencies.
                OrderBy(a => a.Name);

            var result = agencies?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = u.DocumentImage == null ? noDataImagefilePath : $"data:image/*;base64,{Convert.ToBase64String(u.DocumentImage)}",
                    Domain = "<a href=/Vendors/Details?id=" + u.VendorId + ">" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline + ", " + u.District.Name + ", " + u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Pincode = u.PinCode.Code,
                    Status = "<span class='badge badge-light'>" + u.Status.GetEnumDisplayName() + "</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = u.UpdatedBy,
                    VendorName = u.Email,
                    RawStatus = u.Status.GetEnumDisplayName(),
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            allAgencies?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);

            return Ok(result);
        }

        [HttpGet("GetEmpannelled")]
        public async Task<IActionResult> GetEmpannelled()
        {
            var agencies = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Where(v => !v.Deleted);
            var result = agencies?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrEmpty(u.DocumentUrl) ? noDataImagefilePath : u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            agencies?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return Ok(result);
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var vendor = _context.Vendor
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                 .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.Country)
                .Include(i => i.District)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .FirstOrDefault(a => a.VendorId == vendorUser.VendorId && !a.Deleted);

            var services = vendor.VendorInvestigationServiceTypes?
                .OrderBy(s => s.InvestigationServiceType.Name);
            var serviceResponse = new List<AgencyServiceResponse>();
            foreach (var service in services)
            {
                bool isAllDistrict = service.SelectedDistrictIds?.Contains(-1) == true; // how to set this value in case all districts selected
                string pincodes = $"{ALL_PINCODE}";
                string rawPincodes = $"{ALL_PINCODE}";

                serviceResponse.Add(new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.InsuranceType.GetEnumDisplayName(),
                    ServiceType = service.InvestigationServiceType.Name,
                    District = isAllDistrict ? ALL_DISTRICT : string.Join(", ", _context.District.Where(d => service.SelectedDistrictIds.Contains(d.DistrictId)).Select(s => s.Name)),
                    State = service.State.Code,
                    Country = service.Country.Code,
                    Flag = "/flags/" + service.Country.Code.ToLower() + ".png",
                    Pincodes = pincodes,
                    RawPincodes = rawPincodes,
                    Rate = string.Format(Extensions.GetCultureByCountry(service.Country.Code.ToUpper()), "{0:c}", service.Price),
                    UpdatedBy = service.UpdatedBy,
                    Updated = service.Updated.HasValue ? service.Updated.Value.ToString("dd-MM-yyyy") : service.Created.ToString("dd-MM-yyyy"),
                    IsUpdated = service.IsUpdated,
                    LastModified = service.Updated
                });
            }

            vendor.VendorInvestigationServiceTypes?.ToList().ForEach(i => i.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return Ok(serviceResponse);
        }

        [HttpGet("GetCompanyAgencyUser")]
        public async Task<IActionResult> GetCompanyAgencyUser(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var result = await userService.GetCompanyAgencyUsers(userEmail, id);

            return Ok(result);
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var agentWithLoad = await userService.GetAgencyUsers(userEmail);
            return Ok(agentWithLoad);
        }


        [HttpGet("GetAgentWithCases")]
        public async Task<IActionResult> GetAgentWithCases(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = await _context.VendorApplicationUser
                .FirstOrDefaultAsync(c => c.Email == userEmail);
            if (vendorUser == null)
            {
                return NotFound("Vendor user not found.");
            }

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var onboardingEnabled = await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED);

            var vendorAgentsQuery = _context.VendorApplicationUser
                .Include(u => u.Country)
                .Include(u => u.State)
                .Include(u => u.District)
                .Include(u => u.PinCode)
                .Where(c => c.VendorId == vendorUser.VendorId && !c.Deleted && c.Active && c.Role == AppRoles.AGENT);

            if (onboardingEnabled)
            {
                vendorAgentsQuery = vendorAgentsQuery.Where(c => !string.IsNullOrWhiteSpace(c.MobileUId));
            }

            var vendorAgents = await vendorAgentsQuery
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var result = dashboardService.CalculateAgentCaseStatus(userEmail);  // Assume this is async

            var claim = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .Include(c => c.BeneficiaryDetail)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound("Claim not found.");
            }
            string LocationLatitude = claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                ? claim.CustomerDetail.Latitude
                : claim.BeneficiaryDetail.Latitude;

            string LocationLongitude = claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                ? claim.CustomerDetail.Longitude
                : claim.BeneficiaryDetail.Longitude;

            // Use Parallel.ForEach to run agent processing in parallel
            var agentList = new ConcurrentBag<AgentData>(); // Using a thread-safe collection

            await Task.WhenAll(vendorAgents.Select(async agent =>
            {
                int claimCount = result.GetValueOrDefault(agent.Email, 0);
                var agentData = new VendorUserClaim
                {
                    AgencyUser = agent,
                    CurrentCaseCount = claimCount,
                };
                agents.Add(agentData);

                // Get map data asynchronously
                var (distance, distanceInMetre, duration, durationInSec, map) = await customApiCLient.GetMap(
                    double.Parse(agent.AddressLatitude),
                    double.Parse(agent.AddressLongitude),
                    double.Parse(LocationLatitude),
                    double.Parse(LocationLongitude));

                var mapDetails = $"Driving distance: {distance}; Duration: {duration}";

                var agentInfo = new AgentData
                {
                    Id = agent.Id,
                    Photo = agent.ProfilePicture == null
                        ? noUserImagefilePath
                        : $"data:image/*;base64,{Convert.ToBase64String(agent.ProfilePicture)}",
                    Email = agent.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(agent.MobileUId)
                        ? $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a>"
                        : $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a><span title='Onboarding incomplete !!!' data-toggle='tooltip'><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = $"{agent.FirstName} {agent.LastName}",
                    Phone = $"(+{agent.Country.ISDCode}) {agent.PhoneNumber}",
                    Addressline = $"{agent.Addressline}, {agent.District.Name}, {agent.State.Code}, {agent.Country.Code}",
                    Country = agent.Country.Code,
                    Flag = $"/flags/{agent.Country.Code.ToLower()}.png",
                    Active = agent.Active,
                    Roles = agent.UserRole != null
                        ? $"<span class='badge badge-light'>{agent.UserRole.GetEnumDisplayName()}</span>"
                        : "<span class='badge badge-light'>...</span>",
                    Count = claimCount,
                    UpdateBy = agent.UpdatedBy,
                    Role = agent.UserRole.GetEnumDisplayName(),
                    AgentOnboarded = agent.UserRole != AgencyRole.AGENT || !string.IsNullOrWhiteSpace(agent.MobileUId),
                    RawEmail = agent.Email,
                    PersonMapAddressUrl = string.Format(map, "300", "300"),
                    MapDetails = mapDetails,
                    PinCode = agent.PinCode.Code,
                    Distance = distance,
                    DistanceInMetres = distanceInMetre,
                    Duration = duration,
                    DurationInSeconds = durationInSec,
                    AddressLocationInfo = claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                        ? claim.CustomerDetail.AddressLocationInfo
                        : claim.BeneficiaryDetail.AddressLocationInfo
                };

                agentList.Add(agentInfo);
            }));

            return Ok(agentList);
        }
    }

    public class AgentData
    {
        public long Id { get; set; }
        public string Photo { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Addressline { get; set; }
        public bool Active { get; set; }
        public string Roles { get; set; }
        public string Country { get; set; }
        public string? Flag { get; set; }
        public int Count { get; set; }
        public string UpdateBy { get; set; }
        public string Role { get; set; }
        public bool AgentOnboarded { get; set; }
        public string RawEmail { get; set; }
        public string? PersonMapAddressUrl { get; set; }
        public string? MapDetails { get; set; }
        public string PinCode { get; set; }
        public string Distance { get; set; }
        public float DistanceInMetres { get; set; }
        public string Duration { get; set; }
        public int DurationInSeconds { get; set; }
        public string? AddressLocationInfo { get; set; }
    }
}