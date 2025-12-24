using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IVendorService
    {
        Task<object[]> AllAgencies();
        Task<object[]> GetEmpanelledVendorsAsync(ClientCompanyApplicationUser companyUser);
        Task<object[]> GetEmpanelledAgency(ClientCompanyApplicationUser companyUser, long caseId);
        Task<object[]> GetAvailableVendors(string userEmail);
        Task<List<AgencyServiceResponse>> GetAgencyService(long id);
        Task<List<AgencyServiceResponse>> AllServices(string userEmail);
        Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id);
    }

    internal class VendorService : IVendorService
    {
        private readonly string noUserImagefilePath = string.Empty;
        private readonly string noDataImagefilePath = string.Empty;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment env;
        private readonly IFeatureManager featureManager;
        private readonly IDashboardService dashboardService;
        private readonly ICustomApiClient customApiClient;

        public VendorService(ApplicationDbContext context, IWebHostEnvironment env, IFeatureManager featureManager, IDashboardService dashboardService, ICustomApiClient customApiClient)
        {
            noUserImagefilePath = "/img/no-user.png";
            noDataImagefilePath = "/img/no-image.png";
            _context = context;
            this.env = env;
            this.featureManager = featureManager;
            this.dashboardService = dashboardService;
            this.customApiClient = customApiClient;
        }

        public async Task<object[]> GetEmpanelledVendorsAsync(ClientCompanyApplicationUser companyUser)
        {
            var statuses = GetValidStatuses();

            var claimsCases = await GetClaimsAsync(statuses);

            var company = await GetCompanyAsync(companyUser.ClientCompanyId.Value);
            if (company == null) return Array.Empty<object>();

            var result = company.EmpanelledVendors
                .Where(IsActiveVendor)
                .OrderBy(v => v.Name)
                .Select(v => MapVendor(v, companyUser, claimsCases))
                .ToArray();

            ResetVendorUpdateFlags(company.EmpanelledVendors);
            await _context.SaveChangesAsync(null, false);

            return result;
        }
        private static bool IsActiveVendor(Vendor v) => !v.Deleted && v.Status == VendorStatus.ACTIVE;

        private static string[] GetValidStatuses() => new[]
            {
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ALLOCATED_TO_VENDOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_AGENT,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.SUBMITTED_TO_SUPERVISOR,
            CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
            };

        private async Task<List<InvestigationTask>> GetClaimsAsync(string[] statuses) =>
            await _context.Investigations
                .Where(c => c.AssignedToAgency &&
                            !c.Deleted &&
                            c.VendorId.HasValue &&
                            statuses.Contains(c.SubStatus))
                .ToListAsync();

        private async Task<ClientCompany?> GetCompanyAsync(long companyId) =>
            await _context.ClientCompany
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.State)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.District)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.Country)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.PinCode)
                .Include(c => c.EmpanelledVendors).ThenInclude(v => v.ratings)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyId);

        private object MapVendor(Vendor u, ClientCompanyApplicationUser companyUser, List<InvestigationTask> claimsCases)
        {
            return new
            {
                Id = u.VendorId,
                Document = GetVendorDocument(u.DocumentUrl),
                Domain = GetDomain(u, companyUser),
                Name = u.Name,
                Code = u.Code,
                Phone = $"(+{u.Country.ISDCode}) {u.PhoneNumber}",
                Address = u.Addressline,
                District = u.District.Name,
                StateCode = u.State.Code,
                State = u.State.Name,
                CountryCode = u.Country.Code,
                Country = u.Country.Name,
                Flag = $"/flags/{u.Country.Code.ToLower()}.png",
                Updated = (u.Updated ?? u.Created).ToString("dd-MM-yyyy"),
                UpdateBy = u.UpdatedBy,
                CaseCount = claimsCases.Count(c => c.VendorId == u.VendorId),
                RateCount = u.RateCount,
                RateTotal = u.RateTotal,
                RawAddress = $"{u.Addressline}, {u.District.Name}, {u.State.Code}, {u.Country.Code}",
                IsUpdated = u.IsUpdated,
                LastModified = u.Updated
            };
        }

        private string GetDomain(Vendor u, ClientCompanyApplicationUser user) =>
            user.Role == AppRoles.COMPANY_ADMIN
                ? $"<a href='/Company/AgencyDetail?id={u.VendorId}'>{u.Email}</a>"
                : u.Email;

        private string GetVendorDocument(string? documentUrl)
        {
            if (string.IsNullOrWhiteSpace(documentUrl))
                return Applicationsettings.NO_IMAGE;

            var path = Path.Combine(env.ContentRootPath, documentUrl);
            return System.IO.File.Exists(path)
                ? $"data:image/*;base64,{Convert.ToBase64String(System.IO.File.ReadAllBytes(path))}"
                : Applicationsettings.NO_IMAGE;
        }

        private static void ResetVendorUpdateFlags(IEnumerable<Vendor> vendors)
        {
            foreach (var vendor in vendors)
                vendor.IsUpdated = false;
        }

        public async Task<object[]> GetEmpanelledAgency(ClientCompanyApplicationUser companyUser, long caseId)
        {
            var claimsCases = await _context.Investigations.Where(c => c.AssignedToAgency && !c.Deleted && c.VendorId.HasValue && GetValidStatuses().Contains(c.SubStatus)).ToListAsync();

            var company = await _context.ClientCompany
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.State)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.District)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.Country)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.PinCode)
                .Include(c => c.EmpanelledVendors)
                    .ThenInclude(v => v.ratings)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            if (company == null)
            {
                return null!;
            }
            var result = company.EmpanelledVendors?.Where(v => !v.Deleted && v.Status == VendorStatus.ACTIVE).OrderBy(u => u.Name).Select(u => new
            {
                Id = u.VendorId,
                Document = u.DocumentUrl != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, u.DocumentUrl)))) : Applicationsettings.NO_IMAGE,
                Domain = u.Email,
                Name = u.Name,
                Code = u.Code,
                Phone = $"(+{u.Country.ISDCode}) {u.PhoneNumber}",
                Address = $"{u.Addressline}",
                District = u.District.Name,
                State = u.State.Code,
                Country = u.Country.Code,
                Flag = $"/flags/{u.Country.Code.ToLower()}.png",
                Updated = u.Updated?.ToString("dd-MM-yyyy") ?? u.Created.ToString("dd-MM-yyyy"),
                UpdateBy = u.UpdatedBy,
                CaseCount = claimsCases.Count(c => c.VendorId == u.VendorId),
                RateCount = u.RateCount,
                RateTotal = u.RateTotal,
                RawAddress = $"{u.Addressline}, {u.District.Name}, {u.State.Code}, {u.Country.Code}",
                IsUpdated = u.IsUpdated,
                LastModified = u.Updated,
                HasService = GetPinCodeAndServiceForTheCase(caseId, u.VendorId),
            }).ToArray();
            company.EmpanelledVendors?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return result;
        }

        private bool GetPinCodeAndServiceForTheCase(long claimId, long vendorId)
        {
            var selectedCase = _context.Investigations
                .Include(p => p.PolicyDetail)
                .Include(p => p.CustomerDetail)
                .Include(p => p.BeneficiaryDetail)
                .FirstOrDefault(c => c.Id == claimId);

            var serviceType = selectedCase.PolicyDetail.InvestigationServiceTypeId;

            long? countryId;
            long? stateId;
            long? districtId;

            if (selectedCase.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                countryId = selectedCase.CustomerDetail.CountryId;
                stateId = selectedCase.CustomerDetail.StateId;
                districtId = selectedCase.CustomerDetail.DistrictId;
            }
            else
            {
                countryId = selectedCase.BeneficiaryDetail.CountryId;
                stateId = selectedCase.BeneficiaryDetail.StateId;
                districtId = selectedCase.BeneficiaryDetail.DistrictId;
            }

            var vendor = _context.Vendor
                .Include(v => v.VendorInvestigationServiceTypes)
                .FirstOrDefault(v => v.VendorId == vendorId);

            var hasService = vendor?.VendorInvestigationServiceTypes
                .Any(v => v.InvestigationServiceTypeId == serviceType &&
                    v.InsuranceType == selectedCase.PolicyDetail.InsuranceType &&
                            (
                            v.DistrictId == 0 ||
                            v.DistrictId == null ||
                            v.DistrictId == districtId
                            ) &&
                            v.StateId == stateId &&
                            v.CountryId == countryId
                            );
            return hasService ?? false;

        }

        public async Task<object[]> GetAvailableVendors(string userEmail)
        {
            var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = _context.ClientCompany
                .Include(c => c.EmpanelledVendors)
                .FirstOrDefault(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = _context.Vendor
                .Where(v => !company.EmpanelledVendors.Contains(v) && !v.Deleted && v.CountryId == company.CountryId)
                .Include(v => v.VendorApplicationUser)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .OrderBy(u => u.Name)
                .AsQueryable();

            var result =
                availableVendors?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = u.DocumentUrl != null ? string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, u.DocumentUrl)))) : Applicationsettings.NO_IMAGE,
                    Domain = u.Email,
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Name,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdateBy = u.UpdatedBy,
                    CanOnboard = u.Status == VendorStatus.ACTIVE &&
                        u.VendorInvestigationServiceTypes != null &&
                        u.VendorApplicationUser != null &&
                        u.VendorApplicationUser.Count > 0 &&
                        u.VendorInvestigationServiceTypes.Count > 0,
                    VendorName = u.Email,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated,
                    Deletable = u.CreatedUser == userEmail
                })?.ToArray();
            availableVendors?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return result;
        }

        public async Task<List<AgencyServiceResponse>> GetAgencyService(long id)
        {
            var vendor = _context.Vendor
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                 .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.Country)
                .Include(i => i.District)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .FirstOrDefault(a => a.VendorId == id);

            var services = vendor.VendorInvestigationServiceTypes?
               .OrderBy(s => s.InvestigationServiceType.Name);
            var serviceResponse = new List<AgencyServiceResponse>();
            foreach (var service in services)
            {
                bool isAllDistrict = service.SelectedDistrictIds?.Contains(-1) == true; // how to set this value in case all districts selected
                string pincodes = $"{ALL_PINCODE}";
                string rawPincodes = $"{ALL_PINCODE}";
                serviceResponse.Add(new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.InsuranceType.GetEnumDisplayName(),
                    ServiceType = service.InvestigationServiceType.Name,
                    District = isAllDistrict ? ALL_DISTRICT : string.Join(", ", _context.District.Where(d => service.SelectedDistrictIds.Contains(d.DistrictId)).Select(s => s.Name)),
                    StateCode = service.State.Code,
                    State = service.State.Name,
                    CountryCode = service.Country.Code,
                    Country = service.Country.Name,
                    Flag = "/flags/" + service.Country.Code.ToLower() + ".png",
                    Pincodes = pincodes,
                    RawPincodes = rawPincodes,
                    Rate = string.Format(Extensions.GetCultureByCountry(service.Country.Code.ToUpper()), "{0:c}", service.Price),
                    UpdatedBy = service.UpdatedBy,
                    Updated = service.Updated.HasValue ? service.Updated.Value.ToString("dd-MM-yyyy") : service.Created.ToString("dd-MM-yyyy"),
                    IsUpdated = service.IsUpdated,
                    LastModified = service.Updated
                });
            }

            vendor.VendorInvestigationServiceTypes?.ToList().ForEach(i => i.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return serviceResponse;
        }

        public async Task<object[]> AllAgencies()
        {
            var allAgencies = _context.Vendor
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.VendorInvestigationServiceTypes)
                .Where(v => !v.Deleted);

            var agencies = allAgencies.
                OrderBy(a => a.Name);

            var result = agencies?.Select(u =>
                new
                {
                    Id = u.VendorId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? noDataImagefilePath : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(env.ContentRootPath, u.DocumentUrl)))),
                    Domain = "<a href=/Vendors/Details?id=" + u.VendorId + ">" + u.Email + "</a>",
                    Name = u.Name,
                    Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline + ", " + u.District.Name + ", " + u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Pincode = u.PinCode.Code,
                    Status = "<span class='badge badge-light'>" + u.Status.GetEnumDisplayName() + "</span>",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    UpdatedBy = u.UpdatedBy,
                    VendorName = u.Email,
                    RawStatus = u.Status.GetEnumDisplayName(),
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();

            allAgencies?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);

            return result;
        }

        public async Task<List<AgencyServiceResponse>> AllServices(string userEmail)
        {
            var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            var vendor = await _context.Vendor
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.District)
                 .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(v => v.Country)
                .Include(i => i.District)
                .Include(i => i.VendorInvestigationServiceTypes)
                .ThenInclude(i => i.InvestigationServiceType)
                .Include(i => i.State)
                .Include(i => i.VendorInvestigationServiceTypes)
                .FirstOrDefaultAsync(a => a.VendorId == vendorUser.VendorId && !a.Deleted);

            var services = vendor.VendorInvestigationServiceTypes?
                .OrderBy(s => s.InvestigationServiceType.Name);
            var serviceResponse = new List<AgencyServiceResponse>();
            foreach (var service in services)
            {
                bool isAllDistrict = service.SelectedDistrictIds?.Contains(-1) == true; // how to set this value in case all districts selected
                string pincodes = $"{ALL_PINCODE}";
                string rawPincodes = $"{ALL_PINCODE}";

                serviceResponse.Add(new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.InsuranceType.GetEnumDisplayName(),
                    ServiceType = service.InvestigationServiceType.Name,
                    District = isAllDistrict ? ALL_DISTRICT : string.Join(", ", _context.District.Where(d => service.SelectedDistrictIds.Contains(d.DistrictId)).Select(s => s.Name)),
                    StateCode = service.State.Code,
                    State = service.State.Name,
                    CountryCode = service.Country.Code,
                    Country = service.Country.Name,
                    Flag = "/flags/" + service.Country.Code.ToLower() + ".png",
                    Pincodes = pincodes,
                    RawPincodes = rawPincodes,
                    Rate = string.Format(Extensions.GetCultureByCountry(service.Country.Code.ToUpper()), "{0:c}", service.Price),
                    UpdatedBy = service.UpdatedBy,
                    Updated = service.Updated.HasValue ? service.Updated.Value.ToString("dd-MM-yyyy") : service.Created.ToString("dd-MM-yyyy"),
                    IsUpdated = service.IsUpdated,
                    LastModified = service.Updated
                });
            }

            vendor.VendorInvestigationServiceTypes?.ToList().ForEach(i => i.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return serviceResponse;
        }

        public async Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id)
        {
            var vendorUser = await _context.VendorApplicationUser
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var onboardingEnabled = await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED);

            var vendorAgentsQuery = _context.VendorApplicationUser
                .Include(u => u.Country)
                .Include(u => u.State)
                .Include(u => u.District)
                .Include(u => u.PinCode)
                .Where(c => c.VendorId == vendorUser.VendorId && !c.Deleted && c.Active && c.Role == AppRoles.AGENT);

            if (onboardingEnabled)
            {
                vendorAgentsQuery = vendorAgentsQuery.Where(c => !string.IsNullOrWhiteSpace(c.MobileUId));
            }

            var vendorAgents = await vendorAgentsQuery
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var result = await dashboardService.CalculateAgentCaseStatus(userEmail);  // Assume this is async

            var claim = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .Include(c => c.BeneficiaryDetail)
                .FirstOrDefaultAsync(c => c.Id == id);

            string LocationLatitude = claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                ? claim.CustomerDetail.Latitude
                : claim.BeneficiaryDetail.Latitude;

            string LocationLongitude = claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                ? claim.CustomerDetail.Longitude
                : claim.BeneficiaryDetail.Longitude;

            // Use Parallel.ForEach to run agent processing in parallel
            var agentList = new ConcurrentBag<AgentData>(); // Using a thread-safe collection

            await Task.WhenAll(vendorAgents.Select(async agent =>
            {
                int claimCount = result.GetValueOrDefault(agent.Email, 0);
                var agentData = new VendorUserClaim
                {
                    AgencyUser = agent,
                    CurrentCaseCount = claimCount,
                };
                agents.Add(agentData);

                // Get map data asynchronously
                var (distance, distanceInMetre, duration, durationInSec, map) = await customApiClient.GetMap(
                    double.Parse(agent.AddressLatitude),
                    double.Parse(agent.AddressLongitude),
                    double.Parse(LocationLatitude),
                    double.Parse(LocationLongitude));

                var mapDetails = $"Driving distance: {distance}; Duration: {duration}";

                var agentInfo = new AgentData
                {
                    Id = agent.Id,
                    Photo = string.IsNullOrWhiteSpace(agent.ProfilePictureUrl)
                       ? noUserImagefilePath
                       : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                   Path.Combine(env.ContentRootPath, agent.ProfilePictureUrl)))),
                    Email = agent.UserRole == AgencyRole.AGENT && !string.IsNullOrWhiteSpace(agent.MobileUId)
                       ? $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a>"
                       : $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a><span title='Onboarding incomplete !!!' data-toggle='tooltip'><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = $"{agent.FirstName} {agent.LastName}",
                    Phone = $"(+{agent.Country.ISDCode}) {agent.PhoneNumber}",
                    Addressline = $"{agent.Addressline}, {agent.District.Name}, {agent.State.Code}, {agent.Country.Code}",
                    Country = agent.Country.Code,
                    Flag = $"/flags/{agent.Country.Code.ToLower()}.png",
                    Active = agent.Active,
                    Roles = agent.UserRole != null
                       ? $"<span class='badge badge-light'>{agent.UserRole.GetEnumDisplayName()}</span>"
                       : "<span class='badge badge-light'>...</span>",
                    Count = claimCount,
                    UpdateBy = agent.UpdatedBy,
                    Role = agent.UserRole.GetEnumDisplayName(),
                    AgentOnboarded = agent.UserRole != AgencyRole.AGENT || !string.IsNullOrWhiteSpace(agent.MobileUId),
                    RawEmail = agent.Email,
                    PersonMapAddressUrl = string.Format(map, "300", "300"),
                    MapDetails = mapDetails,
                    PinCode = agent.PinCode.Code,
                    Distance = distance,
                    DistanceInMetres = distanceInMetre,
                    Duration = duration,
                    DurationInSeconds = durationInSec,
                    AddressLocationInfo = claim.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                       ? claim.CustomerDetail.AddressLocationInfo
                       : claim.BeneficiaryDetail.AddressLocationInfo
                };

                agentList.Add(agentInfo);

            }));
            return agentList;
        }
    }
}
