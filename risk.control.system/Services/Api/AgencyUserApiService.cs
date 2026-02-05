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
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;
        private readonly IDashboardService dashboardService;
        private readonly IBase64FileService base64FileService;
        private readonly DateTime cutoffTime;
        private readonly IFeatureManager featureManager;

        public AgencyUserApiService(
            IConfiguration config,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IDashboardService dashboardService,
            IBase64FileService base64FileService,
            IFeatureManager featureManager)
        {
            cutoffTime = DateTime.Now.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));
            this.context = context;
            this.env = env;
            this.dashboardService = dashboardService;
            this.base64FileService = base64FileService;
            this.featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetAgencyUsers(string userEmail)
        {
            var now = DateTime.UtcNow;

            // 1. Get vendorId
            var vendor = await context.ApplicationUser
                .AsNoTracking()
                .Where(u => u.Email == userEmail)
                .Select(u => new { u.VendorId })
                .SingleOrDefaultAsync();

            if (vendor == null)
                return new();

            // 2. Active session lookup (ONE query)
            var sessionLookup = await context.UserSessionAlive
                .Where(s => s.Updated >= cutoffTime)
                .GroupBy(s => s.ActiveUser.Email)
                .Select(g => new
                {
                    Email = g.Key,
                    LastSeen = g.Max(x => x.Updated),
                    LoggedOut = g.All(x => x.LoggedOut)
                })
                .ToDictionaryAsync(
                    x => x.Email,
                    x => new { x.LastSeen, x.LoggedOut }
                );

            // 3. Case counts (already optimized 👍)
            var caseCounts = await dashboardService.CalculateAgentCaseStatus(userEmail);

            // 4. Feature flag ONCE
            var loginVerificationEnabled =
                await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);

            // 5. Fetch agency users (projection only)
            var users = await context.ApplicationUser
                .AsNoTracking()
                .Where(u =>
                    u.VendorId == vendor.VendorId &&
                    !u.Deleted &&
                    u.Email != userEmail)
                .OrderBy(u => u.IsUpdated)
                .ThenBy(u => u.Updated)
                .Select(u => new
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
                    Country = new { u.Country.Code, u.Country.ISDCode },
                    State = new { u.State.Code, u.State.Name },
                    District = new { u.District.Name },
                    PinCode = new { u.PinCode.Code, u.PinCode.Name }
                })
                .ToListAsync();

            // 6. Map to response
            var responseTasks = users.Select(async u =>
            {
                sessionLookup.TryGetValue(u.Email, out var session);
                caseCounts.TryGetValue(u.Email, out var count);
                var lastSession = await context.UserSessionAlive
                    .Where(s => s.ActiveUser.Email == u.Email && !s.LoggedOut)
                    .OrderByDescending(s => s.Updated ?? s.Created)
                    .FirstOrDefaultAsync();

                string status, statusName, icon;

                if (lastSession == null)
                {
                    status = "#DED5D5";
                    statusName = "Offline";
                    icon = "fa fa-circle-o";
                }
                else
                {
                    var lastSeen = lastSession.Updated ?? lastSession.Created;
                    var minutesAway = (int)(DateTime.Now - lastSeen).TotalMinutes;

                    (status, statusName, icon) = minutesAway switch
                    {
                        < 1 => ("green", "Online now", "fas fa-circle"),
                        < 5 => ("orange", $"Inactive for {minutesAway} minutes", "fas fa-clock"),
                        < 15 => ("orange", $"Away for {minutesAway} minutes", "far fa-clock"),
                        _ => ("#DED5D5", "Offline", "fa fa-circle-o")
                    };
                }

                var photo = await base64FileService.GetBase64FileAsync(u.ProfilePictureUrl, Applicationsettings.NO_USER);
                return new UserDetailResponse
                {
                    Id = u.Id,
                    Photo = photo,

                    Email = $"<a href=/Agency/EditUser?userId={u.Id}>{u.Email}</a>" +
                        (u.Role == AppRoles.AGENT && string.IsNullOrWhiteSpace(u.MobileUId)
                            ? "<span title=\"Onboarding incomplete !!!\"><i class='fa fa-asterisk asterik-style'></i></span>"
                            : ""),

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
                    Role = u.Role.GetEnumDisplayName(),
                    Roles = u.Role.GetEnumDisplayName(),
                    Count = count,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated ?? u.Created,
                    Updated = (u.Updated ?? u.Created).ToString("dd-MM-yyyy"),
                    UpdatedBy = u.UpdatedBy,
                    AgentOnboarded = u.Role != AppRoles.AGENT || !string.IsNullOrWhiteSpace(u.MobileUId),
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = icon,
                    LoginVerified = loginVerificationEnabled
                        ? !u.IsPasswordChangeRequired
                        : true
                };
            });
            var response = (await Task.WhenAll(responseTasks)).ToList();

            // 7. Batch reset IsUpdated (no tracking, no SaveChanges)
            await context.ApplicationUser
                .Where(u => u.VendorId == vendor.VendorId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(u => u.IsUpdated, false));

            return response;
        }

        public async Task<List<UserDetailResponse>> GetCompanyAgencyUsers(string userEmail, long vendorId)
        {
            var now = DateTime.Now;

            // 1️⃣ Pre-fetch active sessions (latest per user) as a dictionary
            var latestSessions = await context.UserSessionAlive
                .Where(s => s.Updated >= cutoffTime || s.Created >= cutoffTime)
                .Include(s => s.ActiveUser)
                .GroupBy(s => s.ActiveUser.Email)
                .Select(g => new
                {
                    Email = g.Key,
                    LastSeen = g.Max(x => x.Updated ?? x.Created),
                    LoggedOut = g.All(x => x.LoggedOut)
                })
                .ToDictionaryAsync(x => x.Email, x => new { x.LastSeen, x.LoggedOut });

            // 2️⃣ Pre-fetch vendor users
            var vendorUsers = await context.ApplicationUser
                .Where(u => u.VendorId == vendorId && !u.Deleted)
                .Include(u => u.Country)
                .Include(u => u.State)
                .Include(u => u.District)
                .Include(u => u.PinCode)
                .OrderBy(u => u.IsUpdated)
                .ThenBy(u => u.Updated)
                .ToListAsync();

            var activeUsersDetails = new List<UserDetailResponse>();
            var photoTasks = new List<Task<string>>();

            // 3️⃣ Start pre-loading photos asynchronously
            foreach (var user in vendorUsers)
            {
                photoTasks.Add(base64FileService.GetBase64FileAsync(user.ProfilePictureUrl, Applicationsettings.NO_USER));
            }

            var photoResults = await Task.WhenAll(photoTasks);

            // 4️⃣ Map users to UserDetailResponse
            for (int i = 0; i < vendorUsers.Count; i++)
            {
                var user = vendorUsers[i];
                var photoUrl = photoResults[i];

                // Lookup last session
                latestSessions.TryGetValue(user.Email, out var session);

                string status, statusName, icon;
                if (session == null || session.LoggedOut || session.LastSeen == null)
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
                        < 1 => ("green", "Online now", "fas fa-circle"),
                        < 5 => ("orange", $"Inactive for {minutesAway} minutes", "fas fa-clock"),
                        < 15 => ("orange", $"Away for {minutesAway} minutes", "far fa-clock"),
                        _ => ("#DED5D5", "Offline", "fa fa-circle-o")
                    };
                }
                activeUsersDetails.Add(new UserDetailResponse
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = (user.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.Role != AppRoles.AGENT)
                        ? user.Email
                        : user.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    RawEmail = user.Email,
                    Phone = $"(+{user.Country.ISDCode}) {user.PhoneNumber}",
                    Photo = photoUrl,
                    Active = user.Active,
                    Addressline = $"{user.Addressline}, {user.District.Name}",
                    State = user.State.Code,
                    StateName = user.State.Name,
                    Pincode = user.PinCode.Code,
                    PincodeName = $"{user.PinCode.Name} - {user.PinCode.Code}",
                    Country = user.Country.Code,
                    Flag = $"/flags/{user.Country.Code.ToLower()}.png",
                    Roles = user.Role.GetEnumDisplayName(),
                    Updated = (user.Updated ?? user.Created).ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = icon,
                    AgentOnboarded = (user.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.Role != AppRoles.AGENT),
                    Agent = user.Role == AppRoles.AGENT,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated ?? user.Created,
                    LoginVerified = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION)
                        ? !user.IsPasswordChangeRequired
                        : true
                });
            }

            // 5️⃣ Bulk reset IsUpdated
            await context.ApplicationUser
                .Where(u => u.VendorId == vendorId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.IsUpdated, false));

            return activeUsersDetails;
        }
    }
}