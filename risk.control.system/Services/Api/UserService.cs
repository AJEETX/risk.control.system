using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IUserService
    {
        Task<List<UserDetailResponse>> GetUsers(string userEmail);
    }

    internal class UserService : IUserService
    {
        private readonly IConfiguration config;
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IDashboardService dashboardService;
        private readonly DateTime cutoffTime;
        private readonly IFeatureManager featureManager;

        public UserService(IConfiguration config, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, IDashboardService dashboardService, IFeatureManager featureManager)
        {
            this.config = config;
            cutoffTime = DateTime.UtcNow.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
            this.featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetUsers(string userEmail)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));

            // Fetch user session data from the database
            var userSessions = await context.UserSessionAlive
                .Where(u => u.Updated >= cutoffTime) // Filter sessions within the last 15 minutes
                .Include(u => u.ActiveUser)? // Include ActiveUser to access email
                .ToListAsync(); // Materialize data into memory

            var activeUsers = userSessions
                    .GroupBy(u => u.ActiveUser.Email)? // Group by email
                    .Select(g => g.OrderByDescending(u => u.Updated).FirstOrDefault())? // Get the latest session
                    .Where(u => u != null && !u.LoggedOut)?
                     .Select(u => u.ActiveUser.Email)? // Select the user email
                     .ToList(); // Exclude users who have logged out

            var users = context.ApplicationUser
                .Include(a => a.District)
                .Include(a => a.State)
                .Include(a => a.Country)
                .Include(a => a.PinCode)
                    .Where(u => !u.Deleted && u.Email != userEmail);

            var allUsers = users?.OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName);

            var activeUsersDetails = new List<UserDetailResponse>();
            foreach (var user in allUsers)
            {
                var currentOnlineTime = context.UserSessionAlive
                    .Where(a => a.ActiveUser.Email == user.Email && activeUsers.Contains(a.ActiveUser.Email))?
                    .AsEnumerable()? // Brings the data into memory
                    .Select(d => (DateTime?)d.Created) // Use nullable DateTime
                    .DefaultIfEmpty(null) // Provide a default value if no records exist
                    .Max();

                string status = "#DED5D5";
                string statusIcon = "fa fa-circle-o";
                string statusName = "Offline";
                if (currentOnlineTime == null)
                {
                }
                else if (currentOnlineTime != null && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 5 && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 15)
                {
                    status = "orange";
                    statusName = $"Away for {DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                    statusIcon = "far fa-clock";
                }
                else if (DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 1 && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 5)
                {
                    status = "orange";
                    statusName = $"Inactive for {DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                    statusIcon = "fas fa-clock";
                }
                else if (DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 0 && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 1)
                {
                    status = "green";
                    statusName = $"Online now";
                    statusIcon = "fas fa-circle";
                }
                var flag = user.Country == null ? "/flags/in.png" : "/flags/" + user.Country?.Code.ToLower() + ".png";
                var photo = user.ProfilePictureUrl == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(await File.ReadAllBytesAsync(
                    Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl))));
                var logVerified = !await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) || !user.IsPasswordChangeRequired;
                var activeUser = new UserDetailResponse
                {
                    Id = user.Id,
                    Name = user.FirstName + " " + user.LastName,
                    Email = "<a>" + user.Email + "</a>",
                    RawEmail = user.Email,
                    Phone = "(+" + user.Country?.ISDCode + ") " + user.PhoneNumber,
                    Photo = photo,
                    Active = user.Active,
                    Addressline = user?.Addressline ?? "--",
                    District = user?.District?.Name ?? "--",
                    State = user?.State?.Code ?? "--",
                    Country = user?.Country?.Code ?? "--",
                    Flag = flag,
                    Roles = string.Join(",", GetUserRoles(user).Result),
                    Pincode = user?.PinCode?.Code ?? 0,
                    OnlineStatus = status,
                    Updated = user.Updated.HasValue ? user.Updated.Value.ToString("dd-MM-yyyy") : user.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated ??= DateTime.UtcNow,
                    LoginVerified = logVerified
                };
                activeUsersDetails.Add(activeUser);
            }
            users?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }

        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);

            var decoratedRoles = new List<string>();

            foreach (var role in roles)
            {
                var decoratedRole = $"{role}";
                decoratedRoles.Add(decoratedRole);
            }
            return decoratedRoles;
        }
    }
}