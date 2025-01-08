using Amazon.Rekognition.Model;

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
       Task<List<UserDetailResponse>> GetCompanyAgencyUsers(string userEmail, long id);
       Task<List<UserDetailResponse>> GetAgencyUsers(string userEmail);
    }
    public class UserService : IUserService
    {
        private readonly IConfiguration config;
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IDashboardService dashboardService;

        public UserService(IConfiguration config, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IDashboardService dashboardService)
        {
            this.config = config;
            this.context = context;
            this.userManager = userManager;
            this.dashboardService = dashboardService;
        }

        public async Task<List<UserDetailResponse>> GetAgencyUsers(string userEmail)
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

            var vendorUser = context.VendorApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            List<VendorUserClaim> agents = new List<VendorUserClaim>();

            var vendorUsers = context.VendorApplicationUser
                .Include(u => u.Country)
                .Include(u => u.State)
                .Include(u => u.District)
                .Include(u => u.PinCode)
                .Where(c => c.VendorId == vendorUser.VendorId
                && !c.Deleted && c.Email != userEmail);

            var users = vendorUsers?
                .OrderBy(u => u.IsUpdated)
                .ThenBy(u => u.Updated)
                .AsQueryable();
            var result = dashboardService.CalculateAgentCaseStatus(userEmail);

            foreach (var user in users)
            {
                int claimCount = 0;
                if (result.TryGetValue(user.Email, out claimCount))
                {
                    var agentData = new VendorUserClaim
                    {
                        AgencyUser = user,
                        CurrentCaseCount = claimCount,
                    };
                    agents.Add(agentData);
                }
                else
                {
                    var agentData = new VendorUserClaim
                    {
                        AgencyUser = user,
                        CurrentCaseCount = 0,
                    };
                    agents.Add(agentData);
                }
            }
            var activeUsersDetails = new List<UserDetailResponse>();

            foreach (var u in agents)
            {
                var currentOnlineTime = context.UserSessionAlive
                   .Where(a => a.ActiveUser.Email == u.AgencyUser.Email && activeUsers.Contains(a.ActiveUser.Email))?
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
                    Id = u.AgencyUser.Id,
                    Photo = u.AgencyUser.ProfilePicture == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(u.AgencyUser.ProfilePicture)),
                    Email = (u.AgencyUser.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.UserRole != AgencyRole.AGENT) ?
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a>" :
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = u.AgencyUser.FirstName + " " + u.AgencyUser.LastName,
                    Phone = u.AgencyUser.PhoneNumber,
                    Addressline = u.AgencyUser.Addressline + ", " + u.AgencyUser.District.Name + ", " + u.AgencyUser.State.Name + ", " + u.AgencyUser.Country.Code + ", " + u.AgencyUser.PinCode.Code,
                    Active = u.AgencyUser.Active,
                    Roles = u.AgencyUser.UserRole != null ? $"<span class=\"badge badge-light\">{u.AgencyUser.UserRole.GetEnumDisplayName()}</span>" : "<span class=\"badge badge-light\">...</span>",
                    Count = u.CurrentCaseCount,
                    Role = u.AgencyUser.UserRole.GetEnumDisplayName(),
                    AgentOnboarded = (u.AgencyUser.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.UserRole != AgencyRole.AGENT),
                    RawEmail = u.AgencyUser.Email,
                    IsUpdated = u.AgencyUser.IsUpdated,
                    LastModified = u.AgencyUser.Updated.GetValueOrDefault(),
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    Updated = u.AgencyUser.Updated.HasValue ? u.AgencyUser.Updated.Value.ToString("dd-MM-yyyy") : u.AgencyUser.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = u.AgencyUser.UpdatedBy,
                };
                activeUsersDetails.Add(activeUser);
            }
            users?.ToList().ForEach(user => user.IsUpdated = false);

            await context.SaveChangesAsync();
            return activeUsersDetails;
        }

        public async Task<List<UserDetailResponse>> GetCompanyAgencyUsers(string userEmail, long id)
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

            var vendorUsers = context.VendorApplicationUser
                  .Include(u => u.Country)
                  .Include(u => u.State)
                  .Include(u => u.District)
                  .Include(u => u.PinCode)
                  .Where(c => c.VendorId == id && !c.Deleted);

            var users = vendorUsers?
                .OrderBy(u => u.IsUpdated)
                .ThenBy(u => u.Updated)
                .AsQueryable();


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
                    Email = (user.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.UserRole != AgencyRole.AGENT) ?
                    "<a href=/Vendors/EditUser?userId=" + user.Id + ">" + user.Email + "</a>" :
                    "<a href=/Vendors/EditUser?userId=" + user.Id + ">" + user.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    RawEmail = user.Email,
                    Phone = user.PhoneNumber,
                    Photo = user.ProfilePicture == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(user.ProfilePicture)),
                    Active = user.Active,
                    Addressline = user.Addressline + ", " + user.District.Name + ", " + user.State.Name + ", " + user.Country.Code,
                    Pincode = user.PinCode.Code,
                    Roles = string.Join(",", GetUserRoles(user).Result),
                    Updated = user.Updated.HasValue ? user.Updated.Value.ToString("dd-MM-yyyy") : user.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    AgentOnboarded = (user.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.Role != AppRoles.AGENT),
                    Agent = user.Role == AppRoles.AGENT,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated.GetValueOrDefault()
                };
                activeUsersDetails.Add(activeUser);
            }

            users?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync();
            return activeUsersDetails;
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
                    Email = "<a href=/Company/EditUser?userId=" + user.Id + ">" + user.Email + "</a>",
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
                    LastModified = user.Updated.GetValueOrDefault(),
                    Updated = user.Updated.HasValue ? user.Updated.Value.ToString("dd-MM-yyyy") : user.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy
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
