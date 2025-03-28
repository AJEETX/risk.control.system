﻿using System.Globalization;
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
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME},{COMPANY_ADMIN.DISPLAY_NAME},{MANAGER.DISPLAY_NAME},{AGENCY_ADMIN.DISPLAY_NAME},{SUPERVISOR.DISPLAY_NAME}")]
    public class AgencyController : ControllerBase
    {
        private static CultureInfo hindi = new CultureInfo("hi-IN");
        private static NumberFormatInfo hindiNFO = (NumberFormatInfo)hindi.NumberFormat.Clone();
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
            await _context.SaveChangesAsync();
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
                OrderBy(a=>a.Name);

            var result = agencies?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrEmpty(u.DocumentUrl) ? noDataImagefilePath : u.DocumentUrl,
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
                .FirstOrDefault(a => a.VendorId == vendorUser.VendorId && !a.Deleted);

            var services = vendor.VendorInvestigationServiceTypes?
                .OrderBy(s => s.InvestigationServiceType.Name);
            var serviceResponse = new List<AgencyServiceResponse>();
            foreach (var service in services)
            {
                var IsAllDistrict = (service.DistrictId == null);
                string pincodes = $"{ALL_PINCODE}";
                string rawPincodes = $"{ALL_PINCODE}";
                //if (!IsAllDistrict)
                //{
                //    var allPinCodesForDistrict = await _context.PinCode.CountAsync(p => p.DistrictId == service.DistrictId);
                //    if(allPinCodesForDistrict == service.PincodeServices.Count && allPinCodesForDistrict > 1)
                //    {
                //        pincodes = ALL_PINCODE;
                //        rawPincodes = ALL_PINCODE;
                //    }
                //    else
                //    {
                //        pincodes = string.Join(", ", service.PincodeServices.Select(c => c.Pincode).Distinct());
                //        rawPincodes = string.Join(", ", service.PincodeServices.Select(c => c.Name).Distinct());
                //    }
                //}   

                serviceResponse.Add(new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.LineOfBusiness.Name,
                    ServiceType = service.InvestigationServiceType.Name,
                    District = IsAllDistrict ? ALL_DISTRICT : service.District.Name,
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
            await _context.SaveChangesAsync();
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
            if (onboardingEnabled)
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
            foreach (var u in agents)
            {
                string distance, duration, map;
                float distanceInMetre;
                int durationInSec;
                (distance, distanceInMetre, duration, durationInSec, map) = await customApiCLient.GetMap(double.Parse(u.AgencyUser.AddressLatitude), double.Parse(u.AgencyUser.AddressLongitude), double.Parse(LocationLatitude), double.Parse(LocationLongitude));

                var mapDetails = $"Driving distance : {distance}; Duration : {duration}";
                var agentData = new AgentData
                {
                    Id = u.AgencyUser.Id,
                    Photo = u.AgencyUser.ProfilePicture == null ? noUserImagefilePath : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(u.AgencyUser.ProfilePicture)),
                    Email = (u.AgencyUser.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.UserRole != AgencyRole.AGENT) ?
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a>" :
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = u.AgencyUser.FirstName + " " + u.AgencyUser.LastName,
                    Phone = "(+" + u.AgencyUser.Country.ISDCode + ") " + u.AgencyUser.PhoneNumber,
                    Addressline = u.AgencyUser.Addressline + ", " + u.AgencyUser.District.Name + ", " + u.AgencyUser.State.Code + ", " + u.AgencyUser.Country.Code,
                    Country = u.AgencyUser.Country.Code,
                    Flag = "/flags/" + u.AgencyUser.Country.Code.ToLower() + ".png",
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