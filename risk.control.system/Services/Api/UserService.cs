using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface IUserService
    {
        Task<List<UserDetailResponse>> GetUsers(string userEmail);
    }

    internal class UserService : IUserService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly int sessionTimeoutInSeconds;
        private readonly int sessionTimeoutinMinutes;
        private readonly int awayThresholdInMinutes;
        private readonly int onlineThresholdInMinutes;
        private readonly DateTime cutoffTime;
        private readonly IFeatureManager featureManager;

        public UserService(IConfiguration config, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, IFeatureManager featureManager)
        {
            awayThresholdInMinutes = int.Parse(config["LOGIN_SESSION_INACTIVE_MIN"]!);
            onlineThresholdInMinutes = int.Parse(config["LOGIN_SESSION_ACTIVE_MIN"]!);
            sessionTimeoutInSeconds = int.Parse(config["SESSION_TIMEOUT_SEC"]!);
            sessionTimeoutinMinutes = sessionTimeoutInSeconds / 60;
            cutoffTime = DateTime.UtcNow.AddSeconds(-sessionTimeoutInSeconds);
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.userManager = userManager;
            this.featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetUsers(string userEmail)
        {
            var now = DateTime.UtcNow;
            var latestSessions = await context.UserSessionAlive.Where(s => s.Updated >= cutoffTime || s.Created >= cutoffTime).Include(s => s.ActiveUser).GroupBy(s => s.ActiveUser.Email)
                .Select(g => new
                {
                    Email = g.Key!,
                    LastSeen = g.Max(x => x.Updated ?? x.Created),
                    LoggedOut = g.All(x => x.LoggedOut)
                }).ToDictionaryAsync(x => x.Email, x => new { x.LastSeen, x.LoggedOut });
            var users = context.ApplicationUser.Include(a => a.District).Include(a => a.State).Include(a => a.Country).Include(a => a.PinCode).Where(u => !u.Deleted && u.Email != userEmail);
            var allUsers = users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
            var activeUsersDetails = new List<UserDetailResponse>();
            foreach (var user in allUsers)
            {
                latestSessions.TryGetValue(user.Email!, out var session);
                string status, statusName, icon;
                if (session?.LoggedOut != false)
                {
                    status = "#DED5D5"; statusName = "Offline"; icon = "fa fa-circle-o";
                }
                else
                {
                    var minutesAway = (int)(now - session.LastSeen).TotalMinutes;
                    (status, statusName, icon) = minutesAway switch
                    {
                        var m when m < onlineThresholdInMinutes => ("green", "Online now", "fas fa-circle"),
                        var m when m < awayThresholdInMinutes => ("orange", $"Inactive for {m} minutes", "fas fa-clock"),
                        var m when m < sessionTimeoutinMinutes => ("orange", $"Away for {m} minutes", "far fa-clock"),
                        _ => ("#DED5D5", "Offline", "fa fa-circle-o")
                    };
                }
                var activeUser = await MapUsers(user, status, statusName, icon);
                activeUsersDetails.Add(activeUser);
            }
            users?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }
        private async Task<UserDetailResponse> MapUsers(ApplicationUser user, string status, string statusName, string icon)
        {
            return new UserDetailResponse
            {
                Id = user.Id,
                Name = user.FirstName + " " + user.LastName,
                Email = "<a>" + user.Email + "</a>",
                RawEmail = user.Email,
                Phone = "(+" + user.Country?.ISDCode + ") " + user.PhoneNumber,
                Photo = user.ProfilePictureUrl == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl)))),
                Active = user.Active,
                Addressline = user?.Addressline ?? "--",
                District = user?.District?.Name ?? "--",
                State = user?.State?.Code ?? "--",
                Country = user?.Country?.Code ?? "--",
                Flag = user!.Country == null ? "/flags/in.png" : "/flags/" + user.Country?.Code.ToLower() + ".png",
                Roles = string.Join(",", GetUserRoles(user!).Result),
                Pincode = user?.PinCode?.Code ?? 0,
                OnlineStatus = status,
                Updated = user!.Updated ?? user.Created,
                UpdatedBy = user.UpdatedBy,
                OnlineStatusName = statusName,
                OnlineStatusIcon = icon,
                IsUpdated = user.IsUpdated,
                LastModified = user.Updated ?? DateTime.UtcNow,
                LoginVerified = (!await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) || !user.IsPasswordChangeRequired)
            };
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