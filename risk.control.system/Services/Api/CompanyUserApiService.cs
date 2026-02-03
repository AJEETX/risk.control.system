using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

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
        private readonly DateTime cutoffTime;
        private readonly IFeatureManager featureManager;

        public CompanyUserApiService(IConfiguration config, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IFeatureManager featureManager)
        {
            cutoffTime = DateTime.Now.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
            this.featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail)
        {
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

            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var companyUsers = context.ApplicationUser
                .Include(u => u.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .ThenInclude(u => u.Country)
                .Where(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var users = companyUsers
                .Where(u => !u.Deleted && u.Email != userEmail);

            var allUsers = users?
                .OrderBy(u => u.FirstName)
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
                else if (currentOnlineTime != null && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 5 && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 15)
                {
                    status = "orange";
                    statusName = $"Away for {DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                    statusIcon = "far fa-clock";
                }
                else if (DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 1 && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 5)
                {
                    status = "orange";
                    statusName = $"Inactive for {DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                    statusIcon = "fas fa-clock";
                }
                else if (DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 0 && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 1)
                {
                    status = "green";
                    statusName = $"Online now";
                    statusIcon = "fas fa-circle";
                }

                var activeUser = new UserDetailResponse
                {
                    Id = user.Id,
                    Name = user.FirstName + " " + user.LastName,
                    Email = "<a href=/Company/EditUser?userId=" + user.Id + ">" + user.Email + "</a>",
                    RawEmail = user.Email,
                    Phone = "(+" + user.Country.ISDCode + ") " + user.PhoneNumber,
                    Photo = user.ProfilePictureUrl == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl)))),
                    Active = user.Active,
                    Addressline = user.Addressline + ", " + user.District.Name,
                    District = user.District.Name,
                    State = user.State.Code,
                    StateName = user.State.Name,
                    Country = user.Country.Code,
                    Flag = "/flags/" + user.Country.Code.ToLower() + ".png",
                    Role = user.Role.GetEnumDisplayName(),
                    Pincode = user.PinCode.Code,
                    PincodeName = user.PinCode.Name + " - " + user.PinCode.Code,
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated.GetValueOrDefault(),
                    Updated = user.Updated.HasValue ? user.Updated.Value.ToString("dd-MM-yyyy") : user.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    LoginVerified = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !user.IsPasswordChangeRequired : true
                };
                activeUsersDetails.Add(activeUser);
            }

            users?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }

        public async Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail, long id)
        {
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

            var companyUser = await context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var companyUsers = context.ApplicationUser
                .Include(c => c.PinCode)
                .Include(u => u.District)
                .Include(u => u.State)
                .Include(u => u.Country)
                .Where(c => c.ClientCompanyId == id);

            var users = companyUsers
                .Where(u => !u.Deleted && u.Email != userEmail);

            var allUsers = users?
                .OrderBy(u => u.FirstName)
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
                else if (currentOnlineTime != null && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 5 && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 15)
                {
                    status = "orange";
                    statusName = $"Away for {DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                    statusIcon = "far fa-clock";
                }
                else if (DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 1 && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 5)
                {
                    status = "orange";
                    statusName = $"Inactive for {DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes} minutes";
                    statusIcon = "fas fa-clock";
                }
                else if (DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes >= 0 && DateTime.Now.Subtract(currentOnlineTime.GetValueOrDefault()).Minutes < 1)
                {
                    status = "green";
                    statusName = $"Online now";
                    statusIcon = "fas fa-circle";
                }

                var activeUser = new UserDetailResponse
                {
                    Id = user.Id,
                    Name = user.FirstName + " " + user.LastName,
                    Email = $"<a href='/CompanyUser/Edit?userId={user.Id}'>{user.Email}</a>",
                    RawEmail = user.Email,
                    Phone = "(+" + user.Country.ISDCode + ") " + user.PhoneNumber,
                    Photo = user.ProfilePictureUrl == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(webHostEnvironment.ContentRootPath, user.ProfilePictureUrl)))),
                    Active = user.Active,
                    Addressline = user.Addressline + ", " + user.District.Name,
                    District = user.District.Name,
                    State = user.State.Code,
                    StateName = user.State.Name,
                    Country = user.Country.Code,
                    Flag = "/flags/" + user.Country.Code.ToLower() + ".png",
                    Roles = user.Role.GetEnumDisplayName(),
                    Pincode = user.PinCode.Code,
                    PincodeName = user.PinCode.Name + " - " + user.PinCode.Code,
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated.GetValueOrDefault(),
                    Updated = user.Updated.HasValue ? user.Updated.Value.ToString("dd-MM-yyyy") : user.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    LoginVerified = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !user.IsPasswordChangeRequired : true
                };
                activeUsersDetails.Add(activeUser);
            }

            users?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }
    }
}