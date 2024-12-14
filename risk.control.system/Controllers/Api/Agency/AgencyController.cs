using Highsoft.Web.Mvc.Charts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public AgencyController(ApplicationDbContext context, UserManager<VendorApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, IDashboardService dashboardService)
        {
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.webHostEnvironment = webHostEnvironment;
            _context = context;
            noUserImagefilePath = "/img/no-user.png";
            noDataImagefilePath = "/img/no-image.png";
        }

        [HttpGet("AllUsers")]
        public IActionResult AllUsers()
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
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .AsQueryable();

            var result =
                users?.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = "<a href=''>" + u.Email + "</a>",
                    Phone = u.PhoneNumber,
                    Photo = string.IsNullOrWhiteSpace(u.ProfilePictureUrl) ? noUserImagefilePath : u.ProfilePictureUrl,
                    Active = u.Active,
                    Addressline = "<span class='badge badge-light'>" + u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code + "</span>",
                    Pincode = u.PinCode.Code,
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy
                });

            return Ok(result?.ToArray());
        }

        [HttpGet("AllAgencies")]
        public IActionResult AllAgencies()
        {
            var agencies = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .Where(v => !v.Deleted);
            var result =
                agencies
                ?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrEmpty(u.DocumentUrl) ? noDataImagefilePath : u.DocumentUrl,
                    Domain = "<a href=/Vendors/Details?id=" + u.VendorId+">" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = "<span class='badge badge-light'>" + u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code + "</span>",
                    Pincode =  u.PinCode.Code,
                    Status = "<span class='badge badge-light'>"+ u.Status.GetEnumDisplayName() + "</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    Update = u.UpdatedBy
                })
                ?.OrderBy(a => a.Name);

            return Ok(result?.ToArray());
        }
        [HttpGet("GetEmpannelled")]
        public IActionResult GetEmpannelled()
        {
            var agencies = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Where(v => !v.Deleted);
            var result =
                agencies
                ?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrEmpty(u.DocumentUrl) ? noDataImagefilePath : u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy
                })
                ?.OrderBy(a => a.Name);

            return Ok(result?.ToArray());
        }

        [HttpGet("AllServices")]
        public IActionResult AllServices()
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
                    Rate = s.Price,
                    UpdatedBy = s.UpdatedBy,
                    Updated = s.Updated.HasValue ? s.Updated.Value.ToString("dd-MM-yyyy") :  s.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = s.UpdatedBy
                });

            return Ok(result?.ToArray());
        }

        [HttpGet("GetCompanyAgencyUser")]
        public IActionResult GetCompanyAgencyUser(long id)
        {
            var vendorUsers = _context.VendorApplicationUser
                  .Include(u => u.Country)
                  .Include(u => u.State)
                  .Include(u => u.District)
                  .Include(u => u.PinCode)
                  .Where(c => c.VendorId == id && !c.Deleted);

            var users = vendorUsers?
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .AsQueryable();

            var result =
                users?.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    //Email = "<a href=/Vendors/EditUser?userId=" + u.Id +">" + u.Email + "</a>",
                    Email = (u.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.MobileUId) || u.UserRole != AgencyRole.AGENT) ?
                    "<a href=/Vendors/EditUser?userId=" + u.Id + ">" + u.Email + "</a>" :
                    "<a href=/Vendors/EditUser?userId=" + u.Id + ">" + u.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    Phone = u.PhoneNumber,
                    Photo = string.IsNullOrWhiteSpace(u.ProfilePictureUrl) ? noUserImagefilePath : u.ProfilePictureUrl,
                    Addressline = "<span class='badge badge-light'>" + u.Addressline + ", " + u.District.Name + ", " + u.State.Name + ", " + u.Country.Code + "</span>",
                    Pincode = u.PinCode.Code,
                    Active = u.Active,
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    Role = u.UserRole.GetEnumDisplayName(),
                    AgentOnboarded = (u.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.MobileUId) || u.UserRole != AgencyRole.AGENT)
                });

            return Ok(result?.ToArray());
        }

        [HttpGet("GetAgentLoad")]
        public IActionResult GetAgentLoad()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            List<VendorUserClaim> agents = new List<VendorUserClaim>();

            var vendorUsers = _context.VendorApplicationUser
                .Include(u=>u.Country)
                .Include(u=>u.State)
                .Include(u=>u.District)
                .Include(u=>u.PinCode)
                .Where(c => c.VendorId == vendorUser.VendorId && !c.Deleted);

            var users = vendorUsers?
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .AsQueryable();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

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
            var agentWithLoad = agents?
                .Select(u => new
                {
                    Id = u.AgencyUser.Id,
                    Photo = string.IsNullOrWhiteSpace(u.AgencyUser.ProfilePictureUrl) ? noUserImagefilePath : u.AgencyUser.ProfilePictureUrl,
                    Email = (u.AgencyUser.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.UserRole != AgencyRole.AGENT) ?
                    "<a href=/Agency/EditUser?userId=" +u.AgencyUser.Id +">" + u.AgencyUser.Email + "</a>":
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = u.AgencyUser.FirstName + " " + u.AgencyUser.LastName,
                    Phone = u.AgencyUser.PhoneNumber,
                    Addressline = "<span class='badge badge-light'>" + u.AgencyUser.Addressline +", "+ u.AgencyUser.District.Name + ", " +u.AgencyUser.State.Name +", "+u.AgencyUser.Country.Code + ", "+u.AgencyUser.PinCode.Code+" </span>",
                    Active = u.AgencyUser.Active,
                    Roles = u.AgencyUser.UserRole != null ? $"<span class=\"badge badge-light\">{u.AgencyUser.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Count = u.CurrentCaseCount,
                    UpdateBy = u.AgencyUser.UpdatedBy,
                    Role = u.AgencyUser.UserRole.GetEnumDisplayName(),
                    AgentOnboarded =(u.AgencyUser.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.UserRole != AgencyRole.AGENT)
                });
            return Ok(agentWithLoad?.ToArray());
        }

    }
}