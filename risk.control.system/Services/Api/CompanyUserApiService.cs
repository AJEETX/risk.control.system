using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface ICompanyUserApiService
    {
        Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail);

        Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail, long id);
    }

    internal class CompanyUserApiService : ICompanyUserApiService
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IBase64FileService base64FileService;
        private readonly DateTime cutoffTime;
        private readonly IFeatureManager featureManager;

        public CompanyUserApiService(
            IConfiguration config,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IBase64FileService base64FileService,
            IFeatureManager featureManager)
        {
            cutoffTime = DateTime.UtcNow.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.base64FileService = base64FileService;
            this.featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail)
        {
            var now = DateTime.UtcNow;

            // 1️⃣ Get the current company user
            var companyUser = await context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null) return new List<UserDetailResponse>();

            // 2️⃣ Pre-fetch latest sessions per user (active in last 15 min)
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

            // 3️⃣ Fetch company users
            var companyUsers = await context.ApplicationUser.AsNoTracking()
                .Where(u => u.ClientCompanyId == companyUser.ClientCompanyId && !u.Deleted && u.Email != userEmail)
                .Include(u => u.PinCode)
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var activeUsersDetails = new List<UserDetailResponse>();
            for (int i = 0; i < companyUsers.Count; i++)
            {
                var user = companyUsers[i];
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
                var photo = (await base64FileService.GetBase64FileAsync(user.ProfilePictureUrl)) ?? Applicationsettings.NO_USER;
                activeUsersDetails.Add(new UserDetailResponse
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = $"<a href=/Company/EditUser?userId={user.Id}>{user.Email}</a>",
                    RawEmail = user.Email,
                    Phone = $"(+{user.Country.ISDCode}) {user.PhoneNumber}",
                    Photo = photo,
                    Active = user.Active,
                    Addressline = $"{user.Addressline}, {user.District.Name}",
                    District = user.District.Name,
                    State = user.State.Code,
                    StateName = user.State.Name,
                    Country = user.Country.Code,
                    Flag = $"/flags/{user.Country.Code.ToLower()}.png",
                    Role = user.Role.GetEnumDisplayName(),
                    Pincode = user.PinCode.Code,
                    PincodeName = $"{user.PinCode.Name} - {user.PinCode.Code}",
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = icon,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated ?? user.Created,
                    Updated = (user.Updated ?? user.Created).ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    LoginVerified = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION)
                        ? !user.IsPasswordChangeRequired
                        : true
                });
            }

            // 5️⃣ Bulk reset IsUpdated
            await context.ApplicationUser.AsNoTracking()
                .Where(u => u.ClientCompanyId == companyUser.ClientCompanyId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.IsUpdated, false));

            return activeUsersDetails;
        }

        public async Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail, long companyId)
        {
            // Get the company user
            var companyUser = await context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);
            if (companyUser == null) return new List<UserDetailResponse>();

            // Get all active sessions in one query
            var sessionLookup = await context.UserSessionAlive
                .Where(s => s.Updated >= cutoffTime && !s.LoggedOut)
                .Include(s => s.ActiveUser)
                .ToListAsync();

            // Dictionary of last seen per email
            var lastSeenDict = sessionLookup
                .GroupBy(s => s.ActiveUser.Email)
                .ToDictionary(g => g.Key, g => g.Max(s => s.Updated));

            // Fetch all users for the company (excluding deleted and current user)
            var users = await context.ApplicationUser.AsNoTracking()
                .Include(u => u.PinCode)
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .Where(u => u.ClientCompanyId == companyId && !u.Deleted && u.Email != userEmail)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var loginVerificationEnabled = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);

            var now = DateTime.UtcNow;
            var activeUsersDetails = new List<UserDetailResponse>();

            foreach (var user in users)
            {
                // Compute online status
                lastSeenDict.TryGetValue(user.Email, out var lastSeen);
                var minutesAway = lastSeen.HasValue ? (int)(now - lastSeen.Value).TotalMinutes : int.MaxValue;

                var (status, statusName, icon) = minutesAway switch
                {
                    < 1 => ("green", "Online now", "fas fa-circle"),
                    < 5 => ("orange", $"Inactive for {minutesAway} minutes", "fas fa-clock"),
                    < 15 => ("orange", $"Away for {minutesAway} minutes", "far fa-clock"),
                    _ => ("#DED5D5", "Offline", "fa fa-circle-o")
                };

                // Convert photo to base64 (optional: cache this for performance)
                var photo = string.IsNullOrWhiteSpace(user.ProfilePictureUrl)
                    ? Applicationsettings.NO_USER
                    : $"data:image/*;base64,{Convert.ToBase64String(File.ReadAllBytes(Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl)))}";

                activeUsersDetails.Add(new UserDetailResponse
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = $"<a href='/CompanyUser/Edit?userId={user.Id}'>{user.Email}</a>",
                    RawEmail = user.Email,
                    Phone = $"(+{user.Country.ISDCode}) {user.PhoneNumber}",
                    Photo = photo,
                    Active = user.Active,
                    Addressline = $"{user.Addressline}, {user.District.Name}",
                    District = user.District.Name,
                    State = user.State.Code,
                    StateName = user.State.Name,
                    Country = user.Country.Code,
                    Flag = $"/flags/{user.Country.Code.ToLower()}.png",
                    Roles = user.Role.GetEnumDisplayName(),
                    Pincode = user.PinCode.Code,
                    PincodeName = $"{user.PinCode.Name} - {user.PinCode.Code}",
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = icon,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated ?? user.Created,
                    Updated = (user.Updated ?? user.Created).ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    LoginVerified = loginVerificationEnabled ? !user.IsPasswordChangeRequired : true
                });
            }

            // Batch update IsUpdated flag
            users.ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(false);

            return activeUsersDetails;
        }
    }
}