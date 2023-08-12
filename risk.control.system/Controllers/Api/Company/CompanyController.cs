using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers.Api.Company
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompanyController(ApplicationDbContext context)
        {
            _context = context;
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

            var users = company.CompanyApplicationUser.AsQueryable();
            var result =
                users.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " "+ u.LastName,
                    Email = "<a href=''>" + u.Email + "</a>",
                    Phone = u.PhoneNumber,
                    Photo = u.ProfilePictureUrl,
                    Addressline = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name
                });
            await Task.Delay(1000);

            return Ok(result.ToArray());
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
                company.EmpanelledVendors.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name
                });
            await Task.Delay(1000);

            return Ok(result.ToArray());
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
                .Where(v => v.ClientCompanyId != companyUser.ClientCompanyId
                && (v.VendorInvestigationServiceTypes != null) && v.VendorInvestigationServiceTypes.Count > 0)
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
            .AsQueryable();

            var result =
                availableVendors.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = u.DocumentUrl,
                    Domain = "<a href=''>" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name
                });
            return Ok(result);
        }

        [HttpGet("GetCompanyVendors")]
        public async Task<IActionResult> GetCompanyVendors(string id)
        {
            var vendor = _context.Vendor
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.District)
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.State)
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.Country)
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.PinCode)
                .FirstOrDefault(c => c.VendorId == id);

            var users = vendor.VendorApplicationUser.AsQueryable();
            var result =
                users.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + " "+ u.LastName,
                    Email = "<a href=''>" + u.Email + "</a>",
                    Phone = u.PhoneNumber,
                    Photo = u.ProfilePictureUrl,
                    Addressline = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Name
                });
            await Task.Delay(1000);

            return Ok(result.ToArray());
        }

        [HttpGet("AllServices")]
        public async Task<IActionResult> AllServices(string id)
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

            var result = vendor.VendorInvestigationServiceTypes.Select(s => new
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
            });

            return Ok(result);
        }
    }
}