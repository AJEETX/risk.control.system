using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;
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
        private readonly IUserService userService;
        private readonly IConfiguration config;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserService userService, IConfiguration config)
        {
            this._context = context;
            this.userManager = userManager;
            this.userService = userService;
            this.config = config;
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;
            try
            {
                var activeUsersDetails = await userService.GetUsers(userEmail);

                return Ok(activeUsersDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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
                    var cutoffTime = DateTime.Now.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));

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
                    var users = await _context.ApplicationUser
                    .Include(a => a.District)
                    .Include(a => a.State)
                    .Include(a => a.Country)
                    .Include(a => a.PinCode)
                    .Where(a => !a.Deleted && activeUsers.Contains(a.Email) && a.Email != userEmail) // Use Contains for SQL IN
                    .OrderBy(a => a.FirstName)
                    .ThenBy(a => a.LastName)
                    ?.ToListAsync();
                    var activeUsersDetails = new List<UserDetailResponse>();
                    foreach (var user in users)
                    {
                        var currentOnlineTime = _context.UserSessionAlive.Where(a => a.ActiveUser.Email == user.Email).Max(d => d.Created);
                        string status = "green";
                        string statusIcon = "fas fa-circle";
                        string statusName = "Online now";
                        if (DateTime.Now.Subtract(currentOnlineTime).Minutes > 5)
                        {
                            status = "#C3B3B3";
                            statusName = $"Away for {DateTime.Now.Subtract(currentOnlineTime).Minutes} minutes";
                            statusIcon = "far fa-clock";
                        }
                        else if (DateTime.Now.Subtract(currentOnlineTime).Minutes >= 1)
                        {
                            status = "orange";
                            statusName = $"Inactive for {DateTime.Now.Subtract(currentOnlineTime).Minutes} minutes";
                            statusIcon = "fas fa-clock";
                        }

                        var activeUser = new UserDetailResponse
                        {
                            Id = user.Id,
                            Name = user.FirstName + " " + user.LastName,
                            Email = "<a href=/User/Edit?userId=" + user.Id + ">" + user.Email + "</a>",
                            Phone = user.PhoneNumber,
                            Photo = user.ProfilePicture == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(user.ProfilePicture)),
                            Active = user.Active,
                            Addressline = user.Addressline,
                            District = user.District.Name,
                            State = user.State.Name,
                            Country = user.Country.Code,
                            Flag = "/flags/" + user.Country.Code.ToLowerInvariant() + ".png",
                            Roles = string.Join(",", GetUserRoles(user).Result),
                            Pincode = user.PinCode.Code,
                            OnlineStatus = status,
                            OnlineStatusName = statusName,
                            OnlineStatusIcon = statusIcon,
                            IsUpdated = user.IsUpdated,
                            LastModified = user.Updated.GetValueOrDefault()
                        };
                        activeUsersDetails.Add(activeUser);
                    }

                    activeUsersDetails?.ToList().ForEach(u => u.IsUpdated = false);
                    await _context.SaveChangesAsync();
                    return Ok(activeUsersDetails);
                }
                catch (Exception)
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