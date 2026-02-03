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
        private readonly DateTime cutoffTime;
        private readonly IFeatureManager featureManager;

        public AgencyUserApiService(IConfiguration config, ApplicationDbContext context, IWebHostEnvironment env, IDashboardService dashboardService, IFeatureManager featureManager)
        {
            cutoffTime = DateTime.Now.AddMinutes(double.Parse(config["LOGIN_SESSION_TIMEOUT_MIN"]));
            this.context = context;
            this.env = env;
            this.dashboardService = dashboardService;
            this.featureManager = featureManager;
        }

        public async Task<List<UserDetailResponse>> GetAgencyUsers(string userEmail)
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

            var vendorUser = context.ApplicationUser.FirstOrDefault(c => c.Email == userEmail);
            List<VendorUserClaim> agents = new List<VendorUserClaim>();

            var vendorUsers = context.ApplicationUser
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
            var result = await dashboardService.CalculateAgentCaseStatus(userEmail);

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
                    Photo = u.AgencyUser.ProfilePictureUrl == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, u.AgencyUser.ProfilePictureUrl)))),
                    Email = (u.AgencyUser.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.Role != AppRoles.AGENT) ?
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a>" :
                    "<a href=/Agency/EditUser?userId=" + u.AgencyUser.Id + ">" + u.AgencyUser.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = u.AgencyUser.FirstName + " " + u.AgencyUser.LastName,
                    Phone = "(+" + u.AgencyUser.Country.ISDCode + ") " + u.AgencyUser.PhoneNumber,
                    Addressline = u.AgencyUser.Addressline + ", " + u.AgencyUser.District.Name,
                    State = u.AgencyUser.State.Code,
                    StateName = u.AgencyUser.State.Name,
                    Pincode = u.AgencyUser.PinCode.Code,
                    PincodeName = u.AgencyUser.PinCode.Name + " - " + u.AgencyUser.PinCode.Code,
                    Country = u.AgencyUser.Country.Code,
                    Flag = "/flags/" + u.AgencyUser.Country.Code.ToLower() + ".png",
                    Active = u.AgencyUser.Active,
                    Roles = u.AgencyUser.Role != null ? u.AgencyUser.Role.GetEnumDisplayName() : "..",
                    Count = u.CurrentCaseCount,
                    Role = u.AgencyUser.Role.GetEnumDisplayName(),
                    AgentOnboarded = (u.AgencyUser.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(u.AgencyUser.MobileUId) || u.AgencyUser.Role != AppRoles.AGENT),
                    RawEmail = u.AgencyUser.Email,
                    IsUpdated = u.AgencyUser.IsUpdated,
                    LastModified = u.AgencyUser.Updated.GetValueOrDefault(),
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    Updated = u.AgencyUser.Updated.HasValue ? u.AgencyUser.Updated.Value.ToString("dd-MM-yyyy") : u.AgencyUser.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = u.AgencyUser.UpdatedBy,
                    LoginVerified = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !u.AgencyUser.IsPasswordChangeRequired : true
                };
                activeUsersDetails.Add(activeUser);
            }
            vendorUsers?.ToList().ForEach(user => user.IsUpdated = false);

            await context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }

        public async Task<List<UserDetailResponse>> GetCompanyAgencyUsers(string userEmail, long id)
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

            var vendorUsers = context.ApplicationUser
                  .Include(u => u.Country)
                  .Include(u => u.State)
                  .Include(u => u.District)
                  .Include(u => u.PinCode)
                  .Where(c => c.VendorId == id && !c.Deleted);

            var users = vendorUsers?
                .OrderBy(u => u.IsUpdated)
                .ThenBy(u => u.Updated);

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
                    Email = (user.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.Role != AppRoles.AGENT) ?
                    user.Email :
                    user.Email + "</a><span title=\"Onboarding incomplete !!!\" data-toggle=\"tooltip\"><i class='fa fa-asterisk asterik-style'></i></span>",
                    RawEmail = user.Email,
                    Phone = "(+" + user.Country.ISDCode + ") " + user.PhoneNumber,
                    Photo = user.ProfilePictureUrl == null ? Applicationsettings.NO_USER : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, user.ProfilePictureUrl)))),
                    Active = user.Active,
                    Addressline = user.Addressline + ", " + user.District.Name,
                    State = user.State.Code,
                    StateName = user.State.Name,
                    Pincode = user.PinCode.Code,
                    PincodeName = user.PinCode.Name + " - " + user.PinCode.Code,
                    Country = user.Country.Code,
                    Flag = "/flags/" + user.Country.Code.ToLower() + ".png",
                    Roles = user.Role.GetEnumDisplayName(),
                    Updated = user.Updated.HasValue ? user.Updated.Value.ToString("dd-MM-yyyy") : user.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = user.UpdatedBy,
                    OnlineStatus = status,
                    OnlineStatusName = statusName,
                    OnlineStatusIcon = statusIcon,
                    AgentOnboarded = (user.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(user.MobileUId) || user.Role != AppRoles.AGENT),
                    Agent = user.Role == AppRoles.AGENT,
                    IsUpdated = user.IsUpdated,
                    LastModified = user.Updated.GetValueOrDefault(),
                    LoginVerified = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION) ? !user.IsPasswordChangeRequired : true
                };
                activeUsersDetails.Add(activeUser);
            }

            vendorUsers?.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return activeUsersDetails;
        }
    }
}