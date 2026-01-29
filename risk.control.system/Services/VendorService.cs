using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services
{
    public interface IVendorService
    {
        Task<object[]> AllAgencies();

        Task<object[]> GetEmpanelledVendorsAsync(ApplicationUser companyUser);

        Task<object[]> GetEmpanelledAgency(ApplicationUser companyUser, long caseId);

        Task<object[]> GetAvailableVendors(string userEmail);

        Task<List<AgencyServiceResponse>> GetAgencyService(long id);

        Task<List<AgencyServiceResponse>> AllServices(string userEmail);

        Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id);
    }

    internal class VendorService : IVendorService
    {
        private readonly string noUserImagefilePath;
        private readonly string noDataImagefilePath;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment env;
        private readonly IFeatureManager featureManager;
        private readonly IDashboardService dashboardService;
        private readonly IBase64FileService base64FileService;
        private readonly ICustomApiClient customApiClient;

        public VendorService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IFeatureManager featureManager,
            IDashboardService dashboardService,
            IBase64FileService base64FileService,
            ICustomApiClient customApiClient)
        {
            noUserImagefilePath = "/img/no-user.png";
            noDataImagefilePath = "/img/no-image.png";
            _context = context;
            this.env = env;
            this.featureManager = featureManager;
            this.dashboardService = dashboardService;
            this.base64FileService = base64FileService;
            this.customApiClient = customApiClient;
        }

        public async Task<object[]> GetEmpanelledVendorsAsync(ApplicationUser companyUser)
        {
            var statuses = GetValidStatuses();

            var claimsCases = await GetCasesAsync(statuses);

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

        private async Task<List<InvestigationTask>> GetCasesAsync(string[] statuses) =>
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

        private async Task<object> MapVendor(Vendor u, ApplicationUser companyUser, List<InvestigationTask> caseTasks)
        {
            var document = base64FileService.GetBase64FileAsync(u.DocumentUrl, Applicationsettings.NO_IMAGE);

            return new
            {
                Id = u.VendorId,
                Document = await document,
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
                CaseCount = caseTasks.Count(c => c.VendorId == u.VendorId),
                RateCount = u.RateCount,
                RateTotal = u.RateTotal,
                RawAddress = $"{u.Addressline}, {u.District.Name}, {u.State.Code}, {u.Country.Code}",
                IsUpdated = u.IsUpdated,
                LastModified = u.Updated
            };
        }

        private static string GetDomain(Vendor u, ApplicationUser user) =>
            user.Role == AppRoles.COMPANY_ADMIN
                ? $"<a href='/Company/AgencyDetail?id={u.VendorId}'>{u.Email}</a>"
                : u.Email;

        private static void ResetVendorUpdateFlags(IEnumerable<Vendor> vendors)
        {
            foreach (var vendor in vendors)
                vendor.IsUpdated = false;
        }

        public async Task<object[]> GetEmpanelledAgency(ApplicationUser companyUser, long caseId)
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
            var result = company.EmpanelledVendors?.Where(v => !v.Deleted && v.Status == VendorStatus.ACTIVE).OrderBy(u => u.Name)
                .Select(async u =>
                {
                    var hasService = GetPinCodeAndServiceForTheCase(caseId, u.VendorId);
                    var document = base64FileService.GetBase64FileAsync(u.DocumentUrl, Applicationsettings.NO_IMAGE);
                    await Task.WhenAll(document, hasService);
                    return new
                    {
                        Id = u.VendorId,
                        Document = await document,
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
                        HasService = await hasService
                    };
                }).ToList();
            var awaitedResults = await Task.WhenAll(result);
            company.EmpanelledVendors?.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return awaitedResults;
        }

        private async Task<bool> GetPinCodeAndServiceForTheCase(long caseId, long vendorId)
        {
            var selectedCase = await _context.Investigations
                .Include(p => p.PolicyDetail)
                .Include(p => p.CustomerDetail)
                .Include(p => p.BeneficiaryDetail)
                .FirstOrDefaultAsync(c => c.Id == caseId);

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

            var vendor = await _context.Vendor
                .Include(v => v.VendorInvestigationServiceTypes)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);

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
            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);
            var company = await _context.ClientCompany
                .Include(c => c.EmpanelledVendors)
                .FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            var availableVendors = _context.Vendor
                .Where(v => !company.EmpanelledVendors.Contains(v) && !v.Deleted && v.CountryId == company.CountryId)
                .Include(v => v.ApplicationUser)
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
                        u.ApplicationUser != null &&
                        u.ApplicationUser.Count > 0 &&
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
                .FirstOrDefaultAsync(a => a.VendorId == id);

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
                    Rate = string.Format(CustomExtensions.GetCultureByCountry(service.Country.Code.ToUpper()), "{0:c}", service.Price),
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
            var agencyData = await _context.Vendor
         .Include(v => v.Country)
         .Include(v => v.District)
         .Include(v => v.State)
         .Include(v => v.PinCode)
         .Where(v => !v.Deleted)
         .OrderBy(a => a.Name)
         .Select(u => new
         {
             u.VendorId,
             u.DocumentUrl,
             u.Email,
             u.Name,
             u.Code,
             ISDCode = u.Country.ISDCode,
             u.PhoneNumber,
             u.Addressline,
             DistrictName = u.District.Name,
             StateCode = u.State.Code,
             CountryCode = u.Country.Code,
             PinCodeValue = u.PinCode.Code,
             u.Status,
             u.Updated,
             u.Created,
             u.UpdatedBy,
             u.IsUpdated
         })
         .ToListAsync();

            var result = agencyData.Select(u =>
            {
                string documentBase64 = noDataImagefilePath;
                if (!string.IsNullOrWhiteSpace(u.DocumentUrl))
                {
                    var fullPath = Path.Combine(env.ContentRootPath, u.DocumentUrl);
                    if (File.Exists(fullPath))
                    {
                        // Note: Consider caching these bytes if files are accessed frequently
                        documentBase64 = $"data:image/*;base64,{Convert.ToBase64String(File.ReadAllBytes(fullPath))}";
                    }
                }

                return new
                {
                    Id = u.VendorId,
                    Document = documentBase64,
                    Domain = $"<a href='/Vendors/Details?id={u.VendorId}'>{u.Email}</a>",
                    u.Name,
                    u.Code,
                    Phone = $"(+{u.ISDCode}) {u.PhoneNumber}",
                    Address = $"{u.Addressline}, {u.DistrictName}, {u.StateCode}",
                    Country = u.CountryCode,
                    Flag = $"/flags/{u.CountryCode.ToLower()}.png",
                    Pincode = u.PinCodeValue,
                    Status = $"<span class='badge badge-light'>{u.Status.GetEnumDisplayName()}</span>",
                    Updated = (u.Updated ?? u.Created).ToString("dd-MM-yyyy"),
                    u.UpdatedBy,
                    VendorName = u.Email,
                    RawStatus = u.Status.GetEnumDisplayName(),
                    u.IsUpdated,
                    LastModified = u.Updated
                };
            }).ToArray();

            // 3. Batch Update (EF Core 7+) - This is much faster than loading all into memory
            await _context.Vendor
                .Where(v => !v.Deleted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsUpdated, false));

            return result;
        }

        public async Task<List<AgencyServiceResponse>> AllServices(string userEmail)
        {
            // 1. Get VendorId with a lightweight query
            var vendorUser = await _context.ApplicationUser
                .Where(c => c.Email == userEmail)
                .Select(c => new { c.VendorId })
                .FirstOrDefaultAsync();

            if (vendorUser == null) return new List<AgencyServiceResponse>();

            // 2. Fetch services and required data in one go
            var servicesData = await _context.VendorInvestigationServiceType
                .Include(s => s.InvestigationServiceType)
                .Include(s => s.State)
                .Include(s => s.Country)
                .Where(s => s.VendorId == vendorUser.VendorId && !s.Vendor.Deleted)
                .OrderBy(s => s.InvestigationServiceType.Name)
                .ToListAsync();

            // 3. Pre-fetch all distinct District IDs needed to avoid N+1 queries
            var allNeededDistrictIds = servicesData
                .Where(s => s.SelectedDistrictIds != null && !s.SelectedDistrictIds.Contains(-1))
                .SelectMany(s => s.SelectedDistrictIds)
                .Distinct()
                .ToList();

            var districtDict = await _context.District
                .Where(d => allNeededDistrictIds.Contains(d.DistrictId))
                .ToDictionaryAsync(d => d.DistrictId, d => d.Name);

            // 4. Map to Response
            var serviceResponse = servicesData.Select(service =>
            {
                bool isAllDistrict = service.SelectedDistrictIds?.Contains(-1) == true;

                // Resolve district names from our local dictionary instead of the database
                var districtNames = isAllDistrict
                    ? ALL_DISTRICT
                    : string.Join(", ", service.SelectedDistrictIds?
                        .Select(id => districtDict.TryGetValue(id, out var name) ? name : null)
                        .Where(n => n != null) ?? Enumerable.Empty<string>());

                var culture = CustomExtensions.GetCultureByCountry(service.Country.Code.ToUpper());

                return new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.InsuranceType.GetEnumDisplayName(),
                    ServiceType = service.InvestigationServiceType.Name,
                    District = districtNames,
                    StateCode = service.State.Code,
                    State = service.State.Name,
                    CountryCode = service.Country.Code,
                    Country = service.Country.Name,
                    Flag = $"/flags/{service.Country.Code.ToLower()}.png",
                    Pincodes = ALL_PINCODE,
                    RawPincodes = ALL_PINCODE,
                    Rate = string.Format(culture, "{0:c}", service.Price),
                    UpdatedBy = service.UpdatedBy,
                    Updated = (service.Updated ?? service.Created).ToString("dd-MM-yyyy"),
                    IsUpdated = service.IsUpdated,
                    LastModified = service.Updated
                };
            }).ToList();

            // 5. High-performance batch update for the flag
            await _context.VendorInvestigationServiceType
                .Where(s => s.VendorId == vendorUser.VendorId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsUpdated, false));

            return serviceResponse;
        }

        public async Task<ConcurrentBag<AgentData>> GetAgentWithCases(string userEmail, long id)
        {
            var vendorUser = await _context.ApplicationUser
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            List<VendorUserClaim> agents = new List<VendorUserClaim>();
            var onboardingEnabled = await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED);

            var vendorAgentsQuery = _context.ApplicationUser
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

            var caseTask = await _context.Investigations
                .Include(c => c.PolicyDetail)
                .Include(c => c.CustomerDetail)
                .Include(c => c.BeneficiaryDetail)
                .FirstOrDefaultAsync(c => c.Id == id);

            string LocationLatitude;
            if (caseTask.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                LocationLatitude = caseTask.CustomerDetail.Latitude;
            }
            else
            {
                LocationLatitude = caseTask.BeneficiaryDetail.Latitude;
            }

            string LocationLongitude;
            if (caseTask.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING)
            {
                LocationLongitude = caseTask.CustomerDetail.Longitude;
            }
            else
            {
                LocationLongitude = caseTask.BeneficiaryDetail.Longitude;
            }

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
                       : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(File.ReadAllBytes(
                   Path.Combine(env.ContentRootPath, agent.ProfilePictureUrl)))),
                    Email = agent.Role == AppRoles.AGENT && !string.IsNullOrWhiteSpace(agent.MobileUId)
                       ? $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a>"
                       : $"<a href='/Agency/EditUser?agentId={agent.Id}'>{agent.Email}</a><span title='Onboarding incomplete !!!' data-toggle='tooltip'><i class='fa fa-asterisk asterik-style'></i></span>",
                    Name = $"{agent.FirstName} {agent.LastName}",
                    Phone = $"(+{agent.Country.ISDCode}) {agent.PhoneNumber}",
                    Addressline = $"{agent.Addressline}, {agent.District.Name}, {agent.State.Code}, {agent.Country.Code}",
                    Country = agent.Country.Code,
                    Flag = $"/flags/{agent.Country.Code.ToLower()}.png",
                    Active = agent.Active,
                    Roles = agent.Role != null
                       ? $"<span class='badge badge-light'>{agent.Role.GetEnumDisplayName()}</span>"
                       : "<span class='badge badge-light'>...</span>",
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
                    AddressLocationInfo = caseTask.PolicyDetail.InsuranceType == InsuranceType.UNDERWRITING
                       ? caseTask.CustomerDetail.AddressLocationInfo
                       : caseTask.BeneficiaryDetail.AddressLocationInfo
                };

                agentList.Add(agentInfo);
            }));
            return agentList;
        }
    }
}