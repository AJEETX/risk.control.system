using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IAgencyUserApiService
    {
        Task<List<UserDetailResponse>> GetCompanyAgencyUsers(string userEmail, long id);

        Task<List<UserDetailResponse>> GetAgencyUsers(string userEmail);
    }

    internal class AgencyUserApiService : IAgencyUserApiService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IDashboardService _dashboardService;
        private readonly IBase64FileService _base64FileService;
        private readonly DateTime _cutoffTime;
        private readonly IFeatureManager _featureManager;
        private readonly int _sessionTimeoutInSeconds;
        private readonly int _sessionTimeoutinMinutes;
        private readonly int _awayThresholdInMinutes;
        private readonly int _onlineThresholdInMinutes;

        public AgencyUserApiService(
            IConfiguration config,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IDashboardService dashboardService,
            IBase64FileService base64FileService,
            IFeatureManager featureManager)
        {
            _awayThresholdInMinutes = int.Parse(config["LOGIN_SESSION_INACTIVE_MIN"]!);
            _onlineThresholdInMinutes = int.Parse(config["LOGIN_SESSION_ACTIVE_MIN"]!);
            _sessionTimeoutInSeconds = int.Parse(config["SESSION_TIMEOUT_SEC"]!);
            _sessionTimeoutinMinutes = _sessionTimeoutInSeconds / 60;
            _cutoffTime = DateTime.UtcNow.AddSeconds(-_sessionTimeoutInSeconds);
            _contextFactory = contextFactory;
            _dashboardService = dashboardService;
            _base64FileService = base64FileService;
            _featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetAgencyUsers(string userEmail)
        {
            var now = DateTime.UtcNow;
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vendor = await context.ApplicationUser.AsNoTracking().Where(u => u.Email == userEmail).Select(u => new { u.VendorId }).SingleOrDefaultAsync();
            if (vendor == null)
                return new();
            var latestSessions = await context.UserSessionAlive.Where(s => s.Updated >= _cutoffTime || s.Created >= _cutoffTime).Include(s => s.ActiveUser).GroupBy(s => s.ActiveUser.Email)
                .Select(g => new
                {
                    Email = g.Key!,
                    LastSeen = g.Max(x => x.Updated ?? x.Created),
                    LoggedOut = g.All(x => x.LoggedOut)
                }).ToDictionaryAsync(x => x.Email, x => new { x.LastSeen, x.LoggedOut });
            var caseCounts = await _dashboardService.CalculateAgentCaseStatus(userEmail);
            var loginVerificationEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);
            var users = await context.ApplicationUser.AsNoTracking().Where(u => u.VendorId == vendor.VendorId && !u.Deleted && u.Email != userEmail)
                .OrderBy(u => u.IsUpdated).ThenBy(u => u.Updated).Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.ProfilePictureUrl,
                    u.Addressline,
                    u.Active,
                    u.Role,
                    u.IsUpdated,
                    u.Updated,
                    u.Created,
                    u.UpdatedBy,
                    u.MobileUId,
                    u.IsPasswordChangeRequired,
                    Country = new { u.Country!.Code, u.Country.ISDCode },
                    State = new { u.State!.Code, u.State.Name },
                    District = new { u.District!.Name },
                    PinCode = new { u.PinCode!.Code, u.PinCode.Name }
                })
                .ToListAsync();
            var responseTasks = users.Select(async u =>
            {
                caseCounts.TryGetValue(u.Email!, out var count);
                latestSessions.TryGetValue(u.Email!, out var session);
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
                var photo = await _base64FileService.GetBase64FileAsync(u.ProfilePictureUrl!, Applicationsettings.NO_USER);
                return new UserDetailResponse
                {
                    Id = u.Id,
                    Photo = photo,
                    Email = $"{u.Email}" + (u.Role == AppRoles.AGENT && string.IsNullOrWhiteSpace(u.MobileUId) ? "<span title=\"Onboarding incomplete !!!\"><i class='fa fa-asterisk asterik-style'></i></span>" : ""),
                    RawEmail = u.Email,
                    Name = $"{u.FirstName} {u.LastName}",
                    Phone = $"(+{u.Country.ISDCode}) {u.PhoneNumber}",
                    Addressline = $"{u.Addressline}, {u.District.Name}",
                    State = u.State.Code,
                    StateName = u.State.Name,
                    Pincode = u.PinCode.Code,
                    PincodeName = $"{u.PinCode.Name} - {u.PinCode.Code}",
                    Country = u.Country.Code,
                    Flag = $"/flags/{u.Country.Code.ToLower()}.png",
                    Active = u.Active,
                    Role = u.Role!.GetEnumDisplayName(),
                    Count = count,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated ?? u.Created,
                    Updated = (u.Updated ?? u.Created),
                    UpdatedBy = u.UpdatedBy,
                    AgentOnboarded = u.Role != AppRoles.AGENT || !string.IsNullOrWhiteSpace(u.MobileUId),
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = icon,
                    LoginVerified = loginVerificationEnabled ? !u.IsPasswordChangeRequired : true
                };
            });
            var response = (await Task.WhenAll(responseTasks)).ToList();
            await context.ApplicationUser.Where(u => u.VendorId == vendor.VendorId).ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsUpdated, false));
            return response;
        }

        public async Task<List<UserDetailResponse>> GetCompanyAgencyUsers(string userEmail, long vendorId)
        {
            var now = DateTime.UtcNow;
            await using var context = await _contextFactory.CreateDbContextAsync();
            var latestSessions = await context.UserSessionAlive
                .Where(s => s.Updated >= _cutoffTime || s.Created >= _cutoffTime)
                .Include(s => s.ActiveUser)
                .GroupBy(s => s.ActiveUser.Email)
                .Select(g => new
                {
                    Email = g.Key!,
                    LastSeen = g.Max(x => x.Updated ?? x.Created),
                    LoggedOut = g.All(x => x.LoggedOut)
                })
                .ToDictionaryAsync(x => x.Email, x => new { x.LastSeen, x.LoggedOut });
            var vendorUsers = await context.ApplicationUser.AsNoTracking()
                .Where(u => u.VendorId == vendorId && !u.Deleted)
                .Include(u => u.Country)
                .Include(u => u.State)
                .Include(u => u.District)
                .Include(u => u.PinCode)
                .OrderBy(u => u.IsUpdated)
                .ThenBy(u => u.Updated)
                .ToListAsync();
            var activeUsersDetails = new List<UserDetailResponse>();
            for (int i = 0; i < vendorUsers.Count; i++)
            {
                var user = vendorUsers[i];
                latestSessions.TryGetValue(user.Email!, out var session);
                string status, statusName, icon;
                if (session?.LoggedOut != false)
                {
                    status = "#DED5D5";
                    statusName = "Offline";
                    icon = "fa fa-circle-o";
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
                var photo = await _base64FileService.GetBase64FileAsync(user.ProfilePictureUrl!, Applicationsettings.NO_USER);
                activeUsersDetails.Add(new UserDetailResponse
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = (user.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.Role != AppRoles.AGENT)
                        ? user.Email
                        : user.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    RawEmail = user.Email,
                    Phone = $"(+{user.Country!.ISDCode}) {user.PhoneNumber}",
                    Photo = photo,
                    Active = user.Active,
                    Addressline = $"{user.Addressline}, {user.District!.Name}",
                    State = user.State!.Code,
                    StateName = user.State.Name,
                    Pincode = user.PinCode!.Code,
                    PincodeName = $"{user.PinCode.Name} - {user.PinCode.Code}",
                    Country = user.Country.Code,
                    Flag = $"/flags/{user.Country.Code.ToLower()}.png",
                    Role = user.Role!.GetEnumDisplayName(),
                    Updated = (user.Updated ?? user.Created),
                    UpdatedBy = user.UpdatedBy,
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = icon,
                    AgentOnboarded = (user.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.Role != AppRoles.AGENT),
                    Agent = user.Role == AppRoles.AGENT,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated ?? user.Created,
                    LoginVerified = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION)
                        ? !user.IsPasswordChangeRequired
                        : true
                });
            }
            await context.ApplicationUser.Where(u => u.VendorId == vendorId).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsUpdated, false));
            return activeUsersDetails;
        }
    }
}