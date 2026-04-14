using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services.Api
{
    public interface IAgencyInvestigationServiceService
    {
        Task<List<AgencyServiceResponse>> GetAgencyService(long id);

        Task<List<AgencyServiceResponse>> AllServices(string userEmail);
    }

    internal class AgencyInvestigationServiceService(
        IDbContextFactory<ApplicationDbContext> contextFactory) : IAgencyInvestigationServiceService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

        public async Task<List<AgencyServiceResponse>> GetAgencyService(long vendorId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var services = await _context.VendorInvestigationServiceType.AsNoTracking().Include(s => s.InvestigationServiceType).Include(s => s.State).Include(s => s.Country).Where(s => s.VendorId == vendorId).OrderBy(s => s.InvestigationServiceType!.Name)
                .ToListAsync();
            var allDistrictIds = services.Where(s => s.SelectedDistrictIds != null && !s.SelectedDistrictIds.Contains(-1)).SelectMany(s => s.SelectedDistrictIds).Distinct().ToList();
            var districtDict = await _context.District.AsNoTracking().Where(d => allDistrictIds.Contains(d.DistrictId)).ToDictionaryAsync(d => d.DistrictId, d => d.Name);
            var serviceResponse = services.Select(service =>
            {
                bool isAllDistrict = service.SelectedDistrictIds?.Contains(-1) == true;
                var districtNames = isAllDistrict ? ALL_DISTRICT : string.Join(", ", service.SelectedDistrictIds?.Select(id => districtDict.TryGetValue(id, out var name) ? name : null)
                        .Where(n => n != null) ?? Enumerable.Empty<string>());
                var culture = CustomExtensions.GetCultureByCountry(service.Country!.Code.ToUpper());
                return new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.InsuranceType!.GetEnumDisplayName(),
                    ServiceType = service.InvestigationServiceType!.Name,
                    District = districtNames,
                    StateCode = service.State!.Code,
                    State = service.State.Name,
                    CountryCode = service.Country.Code,
                    Country = service.Country.Name,
                    Flag = $"/flags/{service.Country.Code.ToLower()}.png",
                    Rate = string.Format(culture, "{0:c}", service.Price),
                    UpdatedBy = service.UpdatedBy!,
                    Updated = service.Updated ?? service.Created,
                    IsUpdated = service.IsUpdated,
                    LastModified = service.Updated
                };
            }).ToList();
            await _context.VendorInvestigationServiceType.AsNoTracking().Where(s => s.VendorId == vendorId).ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsUpdated, false));
            return serviceResponse;
        }

        public async Task<List<AgencyServiceResponse>> AllServices(string userEmail)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var vendorId = await _context.ApplicationUser.AsNoTracking().Where(u => u.Email == userEmail).Select(u => u.VendorId).SingleOrDefaultAsync();
            if (vendorId == null) return new List<AgencyServiceResponse>();
            var servicesData = await _context.VendorInvestigationServiceType.AsNoTracking().Where(s => s.VendorId == vendorId && !s.Vendor!.Deleted).OrderBy(s => s.InvestigationServiceType!.Name)
                .Select(s => new
                {
                    s.VendorId,
                    s.VendorInvestigationServiceTypeId,
                    s.Price,
                    s.SelectedDistrictIds,
                    s.Updated,
                    s.Created,
                    s.IsUpdated,
                    s.UpdatedBy,
                    InsuranceType = s.InsuranceType,
                    ServiceTypeName = s.InvestigationServiceType!.Name,
                    StateCode = s.State!.Code,
                    StateName = s.State.Name,
                    CountryCode = s.Country!.Code,
                    CountryName = s.Country.Name
                }).ToListAsync();
            var districtIds = servicesData.Where(s => s.SelectedDistrictIds != null && !s.SelectedDistrictIds.Contains(-1)).SelectMany(s => s.SelectedDistrictIds).Distinct().ToHashSet();
            var districtDict = districtIds.Count == 0 ? new Dictionary<long, string>() : await _context.District.AsNoTracking().Where(d => districtIds.Contains(d.DistrictId)).Select(d => new { d.DistrictId, d.Name }).ToDictionaryAsync(d => d.DistrictId, d => d.Name);
            var response = servicesData.Select(service =>
            {
                var districtNames = (service.SelectedDistrictIds?.Contains(-1) == true) ? ALL_DISTRICT : string.Join(", ", service.SelectedDistrictIds?.Select(id => districtDict.GetValueOrDefault(id)).Where(name => name != null) ?? Enumerable.Empty<string>());
                return new AgencyServiceResponse
                {
                    VendorId = service.VendorId,
                    Id = service.VendorInvestigationServiceTypeId,
                    CaseType = service.InsuranceType!.GetEnumDisplayName(),
                    ServiceType = service.ServiceTypeName,
                    District = districtNames,
                    StateCode = service.StateCode,
                    State = service.StateName,
                    CountryCode = service.CountryCode,
                    Country = service.CountryName,
                    Flag = $"/flags/{service.CountryCode.ToLower()}.png",
                    Rate = string.Format(CustomExtensions.GetCultureByCountry(service.CountryCode.ToUpper()), "{0:c}", service.Price),
                    UpdatedBy = service.UpdatedBy!,
                    Updated = service.Updated ?? service.Created,
                    IsUpdated = service.IsUpdated,
                    LastModified = service.Updated
                };
            }).ToList();
            await _context.VendorInvestigationServiceType.AsNoTracking().Where(s => s.VendorId == vendorId).ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsUpdated, false));
            return response;
        }
    }
}