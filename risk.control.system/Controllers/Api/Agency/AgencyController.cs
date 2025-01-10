using System.Net.Http;

using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyController : ControllerBase
    {
        private readonly string noUserImagefilePath = string.Empty;
        private readonly string noDataImagefilePath = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<VendorApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IFeatureManager featureManager;
        private readonly IUserService userService;
        private readonly ICustomApiCLient customApiCLient;
        private static HttpClient httpClient = new();

        public AgencyController(ApplicationDbContext context,
            UserManager<VendorApplicationUser> userManager, 
            IWebHostEnvironment webHostEnvironment,
            IFeatureManager featureManager,
            IUserService userService,
            ICustomApiCLient customApiCLient,
            IDashboardService dashboardService)
        {
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.webHostEnvironment = webHostEnvironment;
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
                .Where(u => !u.Deleted && u.Email != userEmail)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .AsQueryable();

            var result = users?.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = "<a href=''>" + u.Email + "</a>",
                    Phone = "(+"+ u.Country.ISDCode+") " + u.PhoneNumber,
                    Photo = u.ProfilePicture == null ? noUserImagefilePath : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(u.ProfilePicture)) ,
                    Active = u.Active,
                    Addressline = u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code,
                    Pincode = u.PinCode.Code,
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            users?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync();
            return Ok(result);
        }

        [HttpGet("AllAgencies")]
        public async Task<IActionResult> AllAgencies()
        {
            var agencies = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .Where(v => !v.Deleted);
            var result =agencies?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrEmpty(u.DocumentUrl) ? noDataImagefilePath : u.DocumentUrl,
                    Domain = "<a href=/Vendors/Details?id=" + u.VendorId+">" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+"+ u.Country.ISDCode+") " + u.PhoneNumber,
                    Address = u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code,
                    Pincode =  u.PinCode.Code,
                    Status = "<span class='badge badge-light'>"+ u.Status.GetEnumDisplayName() + "</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    Update = u.UpdatedBy,
                    VendorName = u.Email,
                    RawStatus = u.Status.GetEnumDisplayName(),
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            agencies?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync();

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
            var result =agencies?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrEmpty(u.DocumentUrl) ? noDataImagefilePath : u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+"+ u.Country.ISDCode+") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            agencies?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync();
            return Ok(result);
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var vendor = _context.Vendor
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.LineOfBusiness)
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
                .ThenInclude(i => i.PincodeServices)
                .FirstOrDefault(a => a.VendorId == vendorUser.VendorId && !a.Deleted);

            var result = vendor.VendorInvestigationServiceTypes?
                .OrderBy(s => s.InvestigationServiceType.Name)?
                .Select(s => new
                {
                    VendorId = s.VendorId,
                    Id = s.VendorInvestigationServiceTypeId,
                    CaseType = s.LineOfBusiness.Name,
                    ServiceType = s.InvestigationServiceType.Name,
                    District = s.District.Name,
                    State = s.State.Name,
                    Country = s.Country.Name,
                    Pincodes = s.PincodeServices.Count == 0 ?
                    "<span class=\"badge badge-danger\"><img class=\"timer-image\" src=\"/img/timer.gif\" /> </span>" :
                     string.Join("", s.PincodeServices.Select(c => "<span class='badge badge-light'>" + c.Pincode + "</span> ")),
                     RawPincodes = string.Join(", ", s.PincodeServices.Select(c =>  c.Pincode)),
                    Rate = s.Price,
                    UpdatedBy = s.UpdatedBy,
                    Updated = s.Updated.HasValue ? s.Updated.Value.ToString("dd-MM-yyyy") :  s.Created.ToString("dd-MM-yyyy"),
                    IsUpdated = s.IsUpdated,
                    LastModified = s.Updated
                })?.ToArray();
            vendor.VendorInvestigationServiceTypes?.ToList().ForEach(i => i.IsUpdated = false);
            await _context.SaveChangesAsync();
            return Ok(result);
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

        [HttpGet("GetAgentLoad")]
        public async Task<IActionResult> GetAgentLoad(string id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var onboardingEnabled = await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED);

            var vendorUsers = _context.VendorApplicationUser
                .Include(u => u.Country)
                .Include(u => u.State)
                .Include(u => u.District)
                .Include(u => u.PinCode)
                .Where(c => c.VendorId == vendorUser.VendorId && !c.Deleted && c.Active && c.Role == AppRoles.AGENT);
            if(onboardingEnabled)
            {
                vendorUsers = vendorUsers.Where(c => !string.IsNullOrWhiteSpace(c.MobileUId));
            }
            var users = vendorUsers?
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .AsQueryable();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            var claim = _context.ClaimsInvestigation.Include(c => c.PolicyDetail).Include(c => c.CustomerDetail).Include(c => c.BeneficiaryDetail).FirstOrDefault(c => c.ClaimsInvestigationId == id);
            var LocationLatitude = string.Empty;
            var LocationLongitude = string.Empty;
            var addressOfInterest = string.Empty;
            if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH)
            {
                LocationLatitude = claim.CustomerDetail.Latitude;
                LocationLongitude = claim.CustomerDetail.Longitude;
            }
            else
            {
                LocationLatitude = claim.BeneficiaryDetail.Latitude;
                LocationLongitude = claim.BeneficiaryDetail.Longitude;
            }
            foreach (var user in users)
            {
                int claimCount = 0;
                if (result.TryGetValue(user.Email, out claimCount))
                {
                    var agentData = new VendorUserClaim
                    {
                        AgencyUser = user,
                        CurrentCaseCount = claimCount,
                    };
                    agents.Add(agentData);
                }
                else
                {
                    var agentData = new VendorUserClaim
                    {
                        AgencyUser = user,
                        CurrentCaseCount = 0,
                    };
                    agents.Add(agentData);
                }
            }

            var agentList = new List<AgentData>();
            foreach(var u in agents)
            { 
                string distance, duration, map;
                float distanceInMetre;
                int durationInSec;
                var agentExistingDrivingMap = _context.AgentDrivingMap.FirstOrDefault(a=>a.AgentId == u.AgencyUser.Id && a.ClaimsInvestigationId == id);

                if (claim.PolicyDetail.ClaimType == ClaimType.HEALTH && claim.CustomerDetail != null)
                {
                    var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={claim.CustomerDetail.Latitude}&longitude={claim.CustomerDetail.Longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                    var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
                    string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";
                    claim.CustomerDetail.AddressLocationInfo = weatherCustomData;
                }
                else if (claim.PolicyDetail.ClaimType == ClaimType.DEATH && claim.BeneficiaryDetail != null)
                {
                    var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={claim.BeneficiaryDetail.Latitude}&longitude={claim.BeneficiaryDetail.Longitude}&current=temperature_2m,windspeed_10m&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                    var weatherData = await httpClient.GetFromJsonAsync<Weather>(weatherUrl);
                    string weatherCustomData = $"Temperature:{weatherData.current.temperature_2m} {weatherData.current_units.temperature_2m}." +
                $"\r\n" +
                $"\r\nWindspeed:{weatherData.current.windspeed_10m} {weatherData.current_units.windspeed_10m}" +
                $"\r\n" +
                $"\r\nElevation(sea level):{weatherData.elevation} metres";
                    claim.BeneficiaryDetail.AddressLocationInfo = weatherCustomData;
                }
                if (agentExistingDrivingMap == null)
                {
                    (distance, distanceInMetre, duration, durationInSec, map) = await customApiCLient.GetMap(double.Parse(u.AgencyUser.AddressLatitude), double.Parse(u.AgencyUser.AddressLongitude), double.Parse(LocationLatitude), double.Parse(LocationLongitude));

                    var agentDrivingMap = new AgentDrivingMap
                    {
                        AgentId = u.AgencyUser.Id,
                        ClaimsInvestigationId = id,
                        Distance = distance,
                        DistanceInMetres = distanceInMetre,
                        Duration = duration,
                        DurationInSeconds = durationInSec,
                        DrivingMap = map
                    };
                    _context.AgentDrivingMap.Add(agentDrivingMap);
                }
                else
                {
                    distance = agentExistingDrivingMap.Distance;
                    duration = agentExistingDrivingMap.Duration;
                    map = agentExistingDrivingMap.DrivingMap;
                    distanceInMetre = agentExistingDrivingMap.DistanceInMetres.GetValueOrDefault();
                    durationInSec = agentExistingDrivingMap.DurationInSeconds.GetValueOrDefault();
                }

                var mapDetails = $"Driving distance : {distance}; Duration : {duration}";
                var agentData = new AgentData
                {
                    Id = u.AgencyUser.Id,
                    Photo = u.AgencyUser.ProfilePicture == null ? noUserImagefilePath : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(u.AgencyUser.ProfilePicture)) ,
                    Email = (u.AgencyUser.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.UserRole != AgencyRole.AGENT) ?
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a>" :
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = u.AgencyUser.FirstName + " " + u.AgencyUser.LastName,
                    Phone = "(+"+ u.AgencyUser.Country.ISDCode+") " + u.AgencyUser.PhoneNumber,
                    Addressline = u.AgencyUser.Addressline + ", " + u.AgencyUser.District.Name + ", " + u.AgencyUser.State.Code + ", " + u.AgencyUser.Country.Code,
                    Active = u.AgencyUser.Active,
                    Roles = u.AgencyUser.UserRole != null ? $"<span class=\"badge badge-light\">{u.AgencyUser.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Count = u.CurrentCaseCount,
                    UpdateBy = u.AgencyUser.UpdatedBy,
                    Role = u.AgencyUser.UserRole.GetEnumDisplayName(),
                    AgentOnboarded = (u.AgencyUser.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.UserRole != AgencyRole.AGENT),
                    RawEmail = u.AgencyUser.Email,
                    PersonMapAddressUrl = map,
                    MapDetails = mapDetails,
                    PinCode = u.AgencyUser.PinCode.Code,
                    Distance = distance,
                    DistanceInMetres = distanceInMetre,
                    Duration = duration,
                    DurationInSeconds = durationInSec,
                    AddressLocationInfo = claim.PolicyDetail.ClaimType == ClaimType.HEALTH ? claim.CustomerDetail.AddressLocationInfo : claim.BeneficiaryDetail.AddressLocationInfo
                };
                agentList.Add(agentData);
            }
            _context.ClaimsInvestigation.Update(claim);
            await _context.SaveChangesAsync();
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