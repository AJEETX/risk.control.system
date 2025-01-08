using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IUserService
    {
       Task<List<UserDetailResponse>> GetUsers(string userEmail);
       Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail);
    }
    public class UserService : IUserService
    {
        private readonly IConfiguration config;
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public UserService(IConfiguration config, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.config = config;
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<List<UserDetailResponse>> GetCompanyUsers(string userEmail)
        {
            var cutoffTime = DateTime.Now.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));

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

            var companyUser = await context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var company = await context.ClientCompany
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.PinCode)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.Country)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.District)
                .Include(c => c.CompanyApplicationUser)
                .ThenInclude(u => u.State)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var users = company.CompanyApplicationUser
                .Where(u => !u.Deleted && u.Email != userEmail)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)?
                .ToList();

            var activeUsersDetails = new List<UserDetailResponse>();

            foreach(var user in users)
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
                    Email = "<a href=/User/Edit?userId=" + user.Id + ">" + user.Email + "</a>",
                    RawEmail = user.Email,
                    Phone = user.PhoneNumber,
                    Photo = user.ProfilePicture == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(user.ProfilePicture)),
                    Active = user.Active,
                    Addressline = user.Addressline,
                    District = user.District.Name,
                    State = user.State.Name,
                    Country = user.Country.Name,
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

            return activeUsersDetails;
        }

        public async Task<List<UserDetailResponse>> GetUsers(string userEmail)
        {
            var cutoffTime = DateTime.Now.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));

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
                    .Where(u => !u.Deleted && u.Email != userEmail)?
                    .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
                            .ToList();

            var activeUsersDetails = new List<UserDetailResponse>();
            foreach (var user in users)
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
                    Email = "<a href=/User/Edit?userId=" + user.Id + ">" + user.Email + "</a>",
                    RawEmail = user.Email,
                    Phone = user.PhoneNumber,
                    Photo = user.ProfilePicture == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(user.ProfilePicture)),
                    Active = user.Active,
                    Addressline = user.Addressline,
                    District = user.District.Name,
                    State = user.State.Name,
                    Country = user.Country.Name,
                    Roles = string.Join(",", GetUserRoles(user).Result),
                    Pincode = user.PinCode.Code,
                    OnlineStatus = status,
                    Updated = user.Updated.HasValue ? user.Updated.Value.ToString("dd-MM-yyyy") : user.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated.GetValueOrDefault()
                };
                activeUsersDetails.Add(activeUser);
            }
            activeUsersDetails?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync();
            return activeUsersDetails;
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
