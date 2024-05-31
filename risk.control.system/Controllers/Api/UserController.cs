using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PORTAL_ADMIN")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this._context = context;
            this.userManager = userManager;
        }

        [HttpGet("AllUsers")]
        public IActionResult AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (companyUser != null && companyUser.IsSuperAdmin)
            {
                var users = _context.ApplicationUser
                    .Include(a => a.District)
                    .Include(a => a.State)
                    .Include(a => a.Country)
                    .Include(a => a.PinCode)
                        .Where(u => !u.Deleted)?
                        .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                                .ToList();
                var result =
                    users.Select(u =>
                    new
                    {
                        Id = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Email = "<a href=/User/Edit?userId=" + u.Id + ">" + u.Email + "</a>",
                        Phone = u.PhoneNumber,
                        Photo = string.IsNullOrWhiteSpace(u.ProfilePictureUrl) ? Applicationsettings.NO_USER : u.ProfilePictureUrl,
                        Active = u.Active,
                        Addressline = u.Addressline,
                        District = u.District.Name,
                        State = u.State.Name,
                        Country = u.Country.Name,
                        Roles = string.Join(",", GetUserRoles(u).Result),
                        Pincode = u.PinCode.Code,
                    })?.ToList();

                return Ok(result);
            }
            return BadRequest();
        }

        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);

            var decoratedRoles = new List<string>();

            foreach (var role in roles)
            {
                var decoratedRole = "<span class=\"badge badge-light\">" + role + "</span>";
                decoratedRoles.Add(decoratedRole);
            }
            return decoratedRoles;
        }
    }
}