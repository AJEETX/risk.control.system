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
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
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
        public async Task<IActionResult> AllUsers()
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
                var result = users?.Select(u =>
                    new
                    {
                        Id = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Email = "<a href=/User/Edit?userId=" + u.Id + ">" + u.Email + "</a>",
                        Phone = u.PhoneNumber,
                    Photo = u.ProfilePicture == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(u.ProfilePicture)) ,
                        Active = u.Active,
                        Addressline = u.Addressline,
                        District = u.District.Name,
                        State = u.State.Name,
                        Country = u.Country.Name,
                        Roles = string.Join(",", GetUserRoles(u).Result),
                        Pincode = u.PinCode.Code,
                        IsUpdated = u.IsUpdated,
                        LastModified = u.Updated
                    })?.ToList();

                users?.ToList().ForEach(u => u.IsUpdated = false);
                await _context.SaveChangesAsync();
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("ActiveUsers")]
        public async Task<IActionResult> ActiveUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            var companyUser = _context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);

            if (companyUser != null && companyUser.IsSuperAdmin)
            {
                try
                {
                    // Calculate the cutoff time for 15 minutes ago
                    var cutoffTime = DateTime.Now.AddMinutes(-15);

                    // Fetch user session data from the database
                    var userSessions = await _context.UserSessionAlive
                        .Where(u => u.Updated >= cutoffTime) // Filter sessions within the last 15 minutes
                        .Include(u => u.ActiveUser)? // Include ActiveUser to access email
                        .ToListAsync(); // Materialize data into memory

                    var activeUsers = userSessions
                            .GroupBy(u => u.ActiveUser.Email)? // Group by email
                            .Select(g => g.OrderByDescending(u => u.Updated).FirstOrDefault())? // Get the latest session
                            .Where(u => u != null && !u.LoggedOut)?
                             .Select(u => u.ActiveUser.Email)? // Select the user email
                             .ToList(); // Exclude users who have logged out

                    if (activeUsers == null || !activeUsers.Any())
                    {
                        return Ok(new { data = new List<object>() });
                    }
                    // Query users from the database
                    var users =await _context.ApplicationUser
                    .Include(a => a.District)
                    .Include(a => a.State)
                    .Include(a => a.Country)
                    .Include(a => a.PinCode)
                    .Where(a => !a.Deleted && activeUsers.Contains(a.Email)) // Use Contains for SQL IN
                    .OrderBy(a => a.FirstName)
                    .ThenBy(a => a.LastName)
                    ?.ToListAsync();

                var result = users?.Select(u =>
                    new
                    {
                        Id = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Email = "<a href=/User/Edit?userId=" + u.Id + ">" + u.Email + "</a>",
                        Phone = u.PhoneNumber,
                        Photo = u.ProfilePicture == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(u.ProfilePicture)) ,
                        Active = u.Active,
                        Addressline = u.Addressline,
                        District = u.District.Name,
                        State = u.State.Name,
                        Country = u.Country.Name,
                        Roles = string.Join(",", GetUserRoles(u).Result),
                        Pincode = u.PinCode.Code,
                        IsUpdated = u.IsUpdated,
                        LastModified = u.Updated
                    })?.ToList();

                return Ok(result);
                }
                catch (Exception ex)
                {

                    throw;
                }
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