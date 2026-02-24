using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IAgencyAgentService
    {
        Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id);
    }

    internal class AgencyAgentService : IAgencyAgentService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AgencyService> logger;
        private readonly IFeatureManager featureManager;
        private readonly IDashboardService dashboardService;
        private readonly IBase64FileService base64FileService;
        private readonly ICustomApiClient customApiClient;

        public AgencyAgentService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<AgencyService> logger,
            IFeatureManager featureManager,
            IDashboardService dashboardService,
            IBase64FileService base64FileService,
            ICustomApiClient customApiClient)
        {
            _contextFactory = contextFactory;
            this.logger = logger;
            this.featureManager = featureManager;
            this.dashboardService = dashboardService;
            this.base64FileService = base64FileService;
            this.customApiClient = customApiClient;
        }

        public async Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long caseId)
        {
            try
            {
                // 1. Get initial data (Vendor info & Feature flag)
                ApplicationUser vendorUser;
                bool onboardingEnabled;
                using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    vendorUser = await context.ApplicationUser.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Email == userEmail);
                    onboardingEnabled = await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED);
                }

                if (vendorUser == null) return new ConcurrentBag<AgentData>();

                // 2. Fetch Agents (Dispose context immediately after ToListAsync)
                List<ApplicationUser> vendorAgents;
                using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    var query = context.ApplicationUser.AsNoTracking()
                        .Where(u => u.VendorId == vendorUser.VendorId && !u.Deleted && u.Active && u.Role == AppRoles.AGENT);

                    if (onboardingEnabled)
                        query = query.Where(u => !string.IsNullOrWhiteSpace(u.MobileUId));

                    vendorAgents = await query
                        .Include(u => u.Country).Include(u => u.State)
                        .Include(u => u.District).Include(u => u.PinCode)
                        .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                        .ToListAsync();
                }

                // 3. Fetch Case Task
                InvestigationTask caseTask;
                using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    caseTask = await context.Investigations.AsNoTracking()
                        .Include(c => c.PolicyDetail)
                        .Include(c => c.CustomerDetail)
                        .Include(c => c.BeneficiaryDetail)
                        .FirstOrDefaultAsync(c => c.Id == caseId);
                }

                if (caseTask == null) return new ConcurrentBag<AgentData>();

                // 4. External Service calls (Ensure these don't share the 'main' context)
                var agentCaseCounts = await dashboardService.CalculateAgentCaseStatus(userEmail);

                var IsUW = caseTask.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING;
                string locationLat = IsUW ? caseTask.CustomerDetail.Latitude : caseTask.BeneficiaryDetail.Latitude;
                string locationLng = IsUW ? caseTask.CustomerDetail.Longitude : caseTask.BeneficiaryDetail.Longitude;

                var agentList = new ConcurrentBag<AgentData>();

                // 5. Parallel Processing
                var mapTasks = vendorAgents.Select(async (agent) =>
                {
                    int claimCount = agentCaseCounts.GetValueOrDefault(agent.Email, 0);

                    // API calls are safe to parallelize as they don't use DbContext
                    var (distance, distanceInMetre, duration, durationInSec, map) =
                        await customApiClient.GetMap(
                            double.Parse(agent.AddressLatitude),
                            double.Parse(agent.AddressLongitude),
                            double.Parse(locationLat),
                            double.Parse(locationLng));

                    var photo = await base64FileService.GetBase64FileAsync(agent.ProfilePictureUrl, Applicationsettings.NO_IMAGE);
                    var mapDetails = $"Driving distance: {distance}; Duration: {duration}";

                    agentList.Add(new AgentData
                    {
                        Id = agent.Id,
                        Photo = photo,
                        Email = agent.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(agent.MobileUId)
                            ? $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a>"
                            : $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a><span title='Onboarding incomplete !!!' data-toggle='tooltip'><i class='fa fa-asterisk asterik-style'></i></span>",
                        Name = $"{agent.FirstName} {agent.LastName}",
                        Phone = $"(+{agent.Country.ISDCode}) {agent.PhoneNumber}",
                        Addressline = $"{agent.Addressline}, {agent.District.Name}, {agent.State.Code}, {agent.Country.Code}",
                        Country = agent.Country.Code,
                        Flag = $"/flags/{agent.Country.Code.ToLower()}.png",
                        Active = agent.Active,
                        Roles = agent.Role != null ? $"<span class='badge badge-light'>{agent.Role.GetEnumDisplayName()}</span>" : "<span class='badge badge-light'>...</span>",
                        Count = claimCount,
                        UpdateBy = agent.UpdatedBy,
                        Role = agent.Role.GetEnumDisplayName(),
                        AgentOnboarded = agent.Role != AppRoles.AGENT || !string.IsNullOrWhiteSpace(agent.MobileUId),
                        RawEmail = agent.Email,
                        PersonMapAddressUrl = string.Format(map, "300", "300"),
                        MapDetails = mapDetails,
                        PinCode = agent.PinCode.Code,
                        Distance = distance,
                        DistanceInMetres = distanceInMetre,
                        Duration = duration,
                        DurationInSeconds = durationInSec,
                        AddressLocationInfo = IsUW ? caseTask.CustomerDetail.AddressLocationInfo : caseTask.BeneficiaryDetail.AddressLocationInfo
                    });
                });

                await Task.WhenAll(mapTasks);
                return agentList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GetAgentWithCases...{UserEmail}", userEmail);
                throw;
            }
        }
    }
}