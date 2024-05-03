using System.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api.Company
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [Authorize(Roles = "PORTAL_ADMIN,COMPANY_ADMIN")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ClientCompanyApplicationUser> userManager;

        public CompanyController(ApplicationDbContext context, UserManager<ClientCompanyApplicationUser> userManager)
        {
            this.userManager = userManager;
            _context = context;
        }

        [HttpGet("AllCompanies")]
        public async Task<IActionResult> AllCompanies()
        {
            var companies = _context.ClientCompany
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State).OrderBy(o => o.Name);
            var result =
                companies.Select(u =>
                new
                {
                    Id = u.ClientCompanyId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Updated = u.Updated.HasValue ?  u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy")
                });

            return Ok(result?.ToArray());
        }

        [HttpGet("CompanyUsers")]
        public async Task<IActionResult> CompanyUsers(long id)
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var adminUser = _context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            if (!adminUser.IsSuperAdmin)
            {
                return BadRequest();
            }

            var companyUsers = _context.ClientCompanyApplicationUser
                .Include(u => u.PinCode)
                .Include(u => u.Country)
                .Include(u => u.District)
                .Include(u => u.State)
                .Where(c => c.ClientCompanyId == id);

            var users = companyUsers
                .Where(u => !u.Deleted)?
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
                    Photo = string.IsNullOrWhiteSpace(u.ProfilePictureUrl) ? Applicationsettings.NO_USER : u.ProfilePictureUrl,
                    Active = u.Active,
                    Addressline = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Pincode = u.PinCode.Code,
                    Updated = u.Updated.HasValue ?  u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy")
                })?.ToArray();
            return Ok(result);
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.PinCode)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.Country)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.District)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.State)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var users = company.CompanyApplicationUser
                .Where(u => !u.Deleted)
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
                    Photo = string.IsNullOrWhiteSpace(u.ProfilePictureUrl) ? Applicationsettings.NO_USER : u.ProfilePictureUrl,
                    Active = u.Active,
                    Addressline = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Roles = u.UserRole != null ? $"<span class=\"badge badge-light\">{u.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Pincode = u.PinCode.Code,
                    Updated = u.Updated.HasValue ?  u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy")
                })?.ToArray();
            return Ok(result);
        }

        [HttpGet("GetEmpanelledVendors")]
        public async Task<IActionResult> GetEmpanelledVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(c => c.State)
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(c => c.District)
                .Include(c => c.EmpanelledVendors)
                .ThenInclude(c => c.Country)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var result =
                company.EmpanelledVendors?.Where(v => !v.Deleted)
                .OrderBy(u => u.Name)
                .Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy")
                });

            return Ok(result?.ToArray());
        }

        [HttpGet("GetAvailableVendors")]
        public async Task<IActionResult> GetAvailableVendors()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var company = _context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = _context.Vendor
                .Where(v =>
                !v.Clients.Any(c => c.ClientCompanyId == companyUser.ClientCompanyId) &&
                (v.VendorInvestigationServiceTypes != null) && v.VendorInvestigationServiceTypes.Count > 0)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.LineOfBusiness)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.InvestigationServiceType)
                .Include(v => v.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.PincodeServices)
                .Where(v => !v.Deleted)
                .OrderBy(u => u.Name)
                .AsQueryable();

            var result =
                availableVendors?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name,
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy")
                });
            return Ok(result?.ToArray());
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices(long id)
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
                .FirstOrDefault(a => a.VendorId == id);

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
                    Updated = s.Updated.HasValue ? s.Updated.Value.ToString("dd-MM-yyyy") : s.Created.ToString("dd-MM-yyyy")
                });

            return Ok(result?.ToArray());
        }
    }
}