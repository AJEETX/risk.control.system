using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface IAgencyServiceTypeManager
    {
        Task<ServiceCreateEditResult> CreateAsync(VendorInvestigationServiceType service, string currentUserEmail);

        Task<ServiceCreateEditResult> EditAsync(VendorInvestigationServiceType service, string currentUserEmail);
    }

    public class AgencyServiceTypeManager : IAgencyServiceTypeManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AgencyServiceTypeManager> _logger;

        public AgencyServiceTypeManager(ApplicationDbContext context, ILogger<AgencyServiceTypeManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceCreateEditResult> CreateAsync(VendorInvestigationServiceType service, string currentUserEmail)
        {
            if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || service.SelectedDistrictIds == null || service.SelectedDistrictIds.Count == 0)
            {
                return Fail("Invalid data.");
            }

            if (!await IsCountryStateValid(service))
                return Fail("Invalid country/state.");

            bool isAllDistricts = service.SelectedDistrictIds.Contains(-1);

            var existingServices = await _context.VendorInvestigationServiceType
                .Where(v =>
                    v.VendorId == service.VendorId &&
                    v.InsuranceType == service.InsuranceType &&
                    v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                    v.CountryId == service.SelectedCountryId &&
                    v.StateId == service.SelectedStateId)
                .ToListAsync();

            if (IsDuplicate(existingServices, service.SelectedDistrictIds, isAllDistricts))
            {
                MarkUpdated(existingServices, service.SelectedDistrictIds, isAllDistricts);
                await _context.SaveChangesAsync();

                return new ServiceCreateEditResult
                {
                    Success = false,
                    IsAllDistricts = isAllDistricts,
                    Message = isAllDistricts
                        ? $"Service <b> [{ALL_DISTRICT}] </b> already exists for the State!"
                        : "Service already exists for the District!"
                };
            }

            PrepareNewService(service, currentUserEmail);

            _context.VendorInvestigationServiceType.Add(service);
            await _context.SaveChangesAsync();

            return new ServiceCreateEditResult
            {
                Success = true,
                IsAllDistricts = isAllDistricts,
                Message = isAllDistricts
                    ? $"Service  <b>[{ALL_DISTRICT}] </b>  added successfully."
                    : "Service created successfully."
            };
        }

        public async Task<ServiceCreateEditResult> EditAsync(VendorInvestigationServiceType service, string currentUserEmail)
        {
            if (service == null || service.SelectedCountryId < 1 || service.SelectedStateId < 1 || service.SelectedDistrictIds == null || service.SelectedDistrictIds.Count == 0)
            {
                return FailEdit("Invalid service data.");
            }

            bool isAllDistricts = service.SelectedDistrictIds.Contains(-1);

            var existingServices = await _context.VendorInvestigationServiceType
                .AsNoTracking()
                .Where(v =>
                    v.VendorId == service.VendorId &&
                    v.InsuranceType == service.InsuranceType &&
                    v.InvestigationServiceTypeId == service.InvestigationServiceTypeId &&
                    v.CountryId == service.SelectedCountryId &&
                    v.StateId == service.SelectedStateId &&
                    v.VendorInvestigationServiceTypeId != service.VendorInvestigationServiceTypeId)
                .ToListAsync();

            if (IsDuplicate(existingServices, service.SelectedDistrictIds, isAllDistricts))
            {
                MarkDuplicateAsUpdated(existingServices, service.SelectedDistrictIds, isAllDistricts);
                await _context.SaveChangesAsync();

                return new ServiceCreateEditResult
                {
                    Success = false,
                    IsAllDistricts = isAllDistricts,
                    Message = isAllDistricts
                        ? $"Service <b> [{ALL_DISTRICT}] </b> already exists for the State!"
                        : "Service already exists for the District!"
                };
            }

            ApplyEdit(service, currentUserEmail);

            _context.VendorInvestigationServiceType.Update(service);
            await _context.SaveChangesAsync();

            return new ServiceCreateEditResult
            {
                Success = true,
                IsAllDistricts = isAllDistricts,
                Message = isAllDistricts ? $"Service <b>[{ALL_DISTRICT}] </b> updated successfully." : "Service updated successfully."
            };
        }

        private static ServiceCreateEditResult Fail(string message) => new() { Success = false, Message = message };

        private async Task<bool> IsCountryStateValid(VendorInvestigationServiceType service)
        {
            return await _context.Country.AnyAsync(c => c.CountryId == service.SelectedCountryId)
                && await _context.State.AnyAsync(s => s.StateId == service.SelectedStateId);
        }

        private static bool IsDuplicate(IEnumerable<VendorInvestigationServiceType> existing, List<long> districts, bool isAllDistricts)
        {
            if (isAllDistricts)
                return existing.Any(s => s.SelectedDistrictIds.Contains(-1));

            return existing.Any(s =>
                s.SelectedDistrictIds.Intersect(districts).Any());
        }

        private void MarkUpdated(IEnumerable<VendorInvestigationServiceType> existing, List<long> districts, bool isAllDistricts)
        {
            var service = isAllDistricts
                ? existing.First(s => s.SelectedDistrictIds.Contains(-1))
                : existing.First(s => s.SelectedDistrictIds.Intersect(districts).Any());

            service.IsUpdated = true;
            _context.Update(service);
        }

        private static void PrepareNewService(VendorInvestigationServiceType service, string email)
        {
            service.CountryId = service.SelectedCountryId;
            service.StateId = service.SelectedStateId;
            service.IsUpdated = true;
            service.Created = DateTime.UtcNow;
            service.Updated = DateTime.UtcNow;
            service.UpdatedBy = email;
        }

        private static ServiceCreateEditResult FailEdit(string message) => new() { Success = false, Message = message };

        private void MarkDuplicateAsUpdated(IEnumerable<VendorInvestigationServiceType> existing, List<long> districts, bool isAllDistricts)
        {
            var duplicate = isAllDistricts
                ? existing.First(s => s.SelectedDistrictIds.Contains(-1))
                : existing.First(s => s.SelectedDistrictIds.Intersect(districts).Any());

            duplicate.IsUpdated = true;
            _context.VendorInvestigationServiceType.Update(duplicate);
        }

        private static void ApplyEdit(VendorInvestigationServiceType service, string email)
        {
            service.CountryId = service.SelectedCountryId;
            service.StateId = service.SelectedStateId;
            service.IsUpdated = true;
            service.Updated = DateTime.UtcNow;
            service.UpdatedBy = email;
        }
    }
}