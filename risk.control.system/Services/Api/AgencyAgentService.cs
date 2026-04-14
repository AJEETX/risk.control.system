using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Api
{
    public interface IAgencyAgentService
    {
        Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id);
    }

    internal class AgencyAgentService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<AgencyApiService> logger,
        IFeatureManager featureManager,
        IDashboardService dashboardService,
        IBase64FileService base64FileService,
        ICustomApiClient customApiClient) : IAgencyAgentService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
        private readonly ILogger<AgencyApiService> _logger = logger;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly IDashboardService _dashboardService = dashboardService;
        private readonly IBase64FileService _base64FileService = base64FileService;
        private readonly ICustomApiClient _customApiClient = customApiClient;
        public async Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long caseId)
        {
            try
            {
                // 1. Fetch all necessary data in one or two context uses
                var (vendorAgents, caseTask) = await FetchRequiredData(userEmail, caseId);

                if (caseTask == null || !vendorAgents.Any())
                    return new ConcurrentBag<AgentData>();

                var agentCaseCounts = await _dashboardService.CalculateAgentCaseStatus(userEmail);

                // 2. Determine target coordinates once
                var isUW = caseTask.PolicyDetail!.InsuranceType == InsuranceType.UNDERWRITING;
                var targetLocationInfo = isUW ? caseTask.CustomerDetail!.AddressLocationInfo : caseTask.BeneficiaryDetail!.AddressLocationInfo;
                var targetLatitude = isUW ? caseTask.CustomerDetail!.Latitude : caseTask.BeneficiaryDetail!.Latitude;
                var targetLongitude = isUW ? caseTask.CustomerDetail!.Longitude : caseTask.BeneficiaryDetail!.Longitude;

                var targetLat = double.Parse(targetLatitude!);
                var targetLng = double.Parse(targetLongitude!);

                // 3. Parallel Processing of external API calls
                var agentList = new ConcurrentBag<AgentData>();
                var tasks = vendorAgents.Select(async agent =>
                {

                    var data = await MapToAgentData(agent, targetLat, targetLng, agentCaseCounts, targetLocationInfo!);
                    agentList.Add(data);
                });

                await Task.WhenAll(tasks);

                // Return sorted by distance (usually what users want)
                return new ConcurrentBag<AgentData>(agentList.OrderBy(a => a.DistanceInMetres));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAgentWithCases for {UserEmail}", userEmail);
                throw;
            }
        }

        private async Task<(List<ApplicationUser> Agents, InvestigationTask? Case)> FetchRequiredData(string userEmail, long caseId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var vendorUser = await context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (vendorUser == null) return (new List<ApplicationUser>(), null);

            var onboardingEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED);

            var agentsQuery = context.ApplicationUser.AsNoTracking()
                .Where(u => u.VendorId == vendorUser.VendorId && !u.Deleted && u.Active && u.Role == AppRoles.AGENT);

            if (onboardingEnabled)
                agentsQuery = agentsQuery.Where(u => !string.IsNullOrWhiteSpace(u.MobileUId));

            var agents = await agentsQuery
                .Include(u => u.Country).Include(u => u.State)
                .Include(u => u.District).Include(u => u.PinCode)
                .OrderBy(u => u.FirstName).ToListAsync();

            var caseTask = await context.Investigations.AsNoTracking()
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .Include(c => c.BeneficiaryDetail)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            return (agents, caseTask);
        }

        private async Task<AgentData> MapToAgentData(ApplicationUser agent, double tLat, double tLng, Dictionary<string, int> counts, string addressInfo)
        {
            var claimCount = counts?.GetValueOrDefault(agent.Email!, 0) ?? 0;
            var mapTask = _customApiClient.GetMap(double.Parse(agent.AddressLatitude!), double.Parse(agent.AddressLongitude!), tLat, tLng);
            var photoTask = _base64FileService.GetBase64FileAsync(agent.ProfilePictureUrl!, Applicationsettings.NO_IMAGE);
            await Task.WhenAll(mapTask, photoTask);
            var (dist, distMetre, dur, durSec, mapUrl) = await mapTask;
            return new AgentData
            {
                Id = agent.Id,
                Photo = await photoTask,
                Name = $"{agent.FirstName} {agent.LastName}",
                Phone = $"(+{agent.Country!.ISDCode}) {agent.PhoneNumber}",
                Addressline = $"{agent.Addressline}, {agent.District!.Name}, {agent.State!.Code}, {agent.Country.Code}",
                Country = agent.Country.Code,
                Flag = $"/flags/{agent.Country.Code.ToLower()}.png",
                Active = agent.Active,
                Count = claimCount,
                UpdateBy = agent.UpdatedBy ?? "",
                AgentOnboarded = !string.IsNullOrWhiteSpace(agent.MobileUId),
                RawEmail = agent.Email!,
                PersonMapAddressUrl = string.Format(mapUrl, "300", "300"),
                MapDetails = $"Driving distance: {dist}; Duration: {dur}",
                PinCode = agent.PinCode!.Code,
                Distance = dist,
                DistanceInMetres = distMetre,
                Duration = dur,
                DurationInSeconds = durSec,
                AddressLocationInfo = addressInfo
            };
        }
    }
}