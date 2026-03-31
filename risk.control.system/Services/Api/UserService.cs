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
        private string status = "#DED5D5", statusIcon = "fa fa-circle-o", statusName = "Offline";
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly DateTime cutoffTime;
        private readonly IFeatureManager featureManager;

        public UserService(IConfiguration config, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, IFeatureManager featureManager)
        {
            cutoffTime = DateTime.UtcNow.AddSeconds(-double.Parse(config["SESSION_TIMEOUT_SEC"]!));
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.userManager = userManager;
            this.featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetUsers(string userEmail)
        {
            var activeUsers = await context.UserSessionAlive.AsNoTracking().Where(u => u.Updated >= cutoffTime && !u.LoggedOut).GroupBy(u => u.ActiveUser.Email).Select(g => g.Key).ToListAsync();
            var users = context.ApplicationUser.Include(a => a.District).Include(a => a.State).Include(a => a.Country).Include(a => a.PinCode).Where(u => !u.Deleted && u.Email != userEmail);
            var allUsers = users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
            var activeUsersDetails = new List<UserDetailResponse>();
            foreach (var user in allUsers)
            {
                var currentOnlineTime = context.UserSessionAlive.Where(a => a.ActiveUser.Email == user.Email && activeUsers.Contains(a.ActiveUser.Email))?.AsEnumerable()?.Select(d => (DateTime?)d.Created).DefaultIfEmpty(null).Max();
                if (currentOnlineTime != null && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 5 && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 15)
                {
                    status = "orange"; statusIcon = "far fa-clock"; statusName = $"Away for {DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                }
                else if (DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 1 && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 5)
                {
                    status = "orange"; statusIcon = "fas fa-clock"; statusName = $"Inactive for {DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                }
                else if (DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 0 && DateTime.UtcNow.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 1)
                {
                    status = "green"; statusName = $"Online now"; statusIcon = "fas fa-circle";
                }
                var activeUser = await MapUsers(user);
                activeUsersDetails.Add(activeUser);
            }
            users?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }
        private async Task<UserDetailResponse> MapUsers(ApplicationUser user)
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
                OnlineStatusIcon = statusIcon,
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