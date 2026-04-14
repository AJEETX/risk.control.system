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
        private readonly ApplicationDbContext _context;
        private readonly IBase64FileService _base64FileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly int _sessionTimeoutInSeconds;
        private readonly int _sessionTimeoutinMinutes;
        private readonly int _awayThresholdInMinutes;
        private readonly int _onlineThresholdInMinutes;
        private readonly DateTime _cutoffTime;
        private readonly IFeatureManager _featureManager;

        public UserService(IConfiguration config,
            ApplicationDbContext context,
            IBase64FileService base64FileService,
            UserManager<ApplicationUser> userManager,
            IFeatureManager featureManager)
        {
            _awayThresholdInMinutes = int.Parse(config["LOGIN_SESSION_INACTIVE_MIN"]!);
            _onlineThresholdInMinutes = int.Parse(config["LOGIN_SESSION_ACTIVE_MIN"]!);
            _sessionTimeoutInSeconds = int.Parse(config["SESSION_TIMEOUT_SEC"]!);
            _sessionTimeoutinMinutes = _sessionTimeoutInSeconds / 60;
            _cutoffTime = DateTime.UtcNow.AddSeconds(-_sessionTimeoutInSeconds);
            _context = context;
            _base64FileService = base64FileService;
            _userManager = userManager;
            _featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetUsers(string userEmail)
        {
            var now = DateTime.UtcNow;
            var latestSessions = await _context.UserSessionAlive.Where(s => s.Updated >= _cutoffTime || s.Created >= _cutoffTime).Include(s => s.ActiveUser).GroupBy(s => s.ActiveUser.Email)
                .Select(g => new
                {
                    Email = g.Key!,
                    LastSeen = g.Max(x => x.Updated ?? x.Created),
                    LoggedOut = g.All(x => x.LoggedOut)
                }).ToDictionaryAsync(x => x.Email, x => new { x.LastSeen, x.LoggedOut });
            var users = _context.ApplicationUser.Include(a => a.District).Include(a => a.State).Include(a => a.Country).Include(a => a.PinCode).Where(u => !u.Deleted && u.Email != userEmail);
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
                        var m when m < _onlineThresholdInMinutes => ("green", "Online now", "fas fa-circle"),
                        var m when m < _awayThresholdInMinutes => ("orange", $"Inactive for {m} minutes", "fas fa-clock"),
                        var m when m < _sessionTimeoutinMinutes => ("orange", $"Away for {m} minutes", "far fa-clock"),
                        _ => ("#DED5D5", "Offline", "fa fa-circle-o")
                    };
                }
                var activeUser = await MapUsers(user, status, statusName, icon);
                activeUsersDetails.Add(activeUser);
            }
            users?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }
        private async Task<UserDetailResponse> MapUsers(ApplicationUser user, string status, string statusName, string icon)
        {
            return new UserDetailResponse
            {
                Id = user.Id,
                Name = $"{user.FirstName} {user.LastName}",
                Email = $"<a>{user.Email}</a>",
                RawEmail = user.Email,
                Phone = $"(+{user.Country?.ISDCode}) {user.PhoneNumber}",
                Photo = await GetUserPhotoBase64(user.ProfilePictureUrl),
                Active = user.Active,
                Addressline = user.Addressline ?? "--",
                District = user.District?.Name ?? "--",
                State = user.State?.Code ?? "--",
                Country = user.Country?.Code ?? "--",
                Flag = GetCountryFlagPath(user.Country?.Code),
                Roles = string.Join(",", await GetUserRoles(user)),
                Pincode = user.PinCode?.Code ?? 0,
                OnlineStatus = status,
                Updated = user.Updated ?? user.Created,
                UpdatedBy = user.UpdatedBy,
                OnlineStatusName = statusName,
                OnlineStatusIcon = icon,
                IsUpdated = user.IsUpdated,
                LastModified = user.Updated ?? DateTime.UtcNow,
                LoginVerified = await IsLoginVerified(user)
            };
        }
        private async Task<string> GetUserPhotoBase64(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return Applicationsettings.NO_USER;

            try
            {
                var photo = await _base64FileService.GetBase64FileAsync(url!, Applicationsettings.NO_USER);
                return photo;
            }
            catch
            {
                return Applicationsettings.NO_USER;
            }
        }

        private string GetCountryFlagPath(string? countryCode)
        {
            var code = countryCode?.ToLower() ?? "in";
            return $"/flags/{code}.png";
        }

        private async Task<bool> IsLoginVerified(ApplicationUser user)
        {
            var checkEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);
            return !checkEnabled || !user.IsPasswordChangeRequired;
        }
        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

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