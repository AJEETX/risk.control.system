using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;

namespace risk.control.system.Controllers.Api.Agency
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgencyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AgencyController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var vendorUser = _context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            var vendor = _context.Vendor
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.District)
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.State)
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.Country)
                .Include(c => c.VendorApplicationUser)
                .ThenInclude(u => u.PinCode)
                .FirstOrDefault(c => c.VendorId == vendorUser.VendorId);

            var users = vendor.VendorApplicationUser.AsQueryable();
            var result =
                users.Select(u =>
                new
                {
                    Id = u.Id,
                    Name = u.FirstName + u.LastName,
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

        [HttpGet("AllAgencies")]
        public async Task<IActionResult> AllAgencies()
        {
            var agencies = await _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes).ToListAsync();
            var result =
                agencies.Select(u =>
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
    }
}