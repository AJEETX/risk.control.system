using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface IAgencyServiceService
    {
        Task<VendorInvestigationServiceType> PrepareCreateViewModelAsync(string userEmail);

        Task<VendorInvestigationServiceType> PrepareCreateAsync(long id);

        Task<VendorInvestigationServiceType> PrepareEditViewModelAsync(long id);

        Task<bool> DeleteServiceAsync(long id);
    }

    internal class AgencyServiceService : IAgencyServiceService
    {
        private readonly ApplicationDbContext _context;

        public AgencyServiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<VendorInvestigationServiceType> PrepareCreateViewModelAsync(string userEmail)
        {
            var vendorUser = await _context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (vendorUser == null) return null;

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);

            if (vendor == null) return null;

            return new VendorInvestigationServiceType
            {
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                Currency = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
            };
        }

        public async Task<VendorInvestigationServiceType> PrepareEditViewModelAsync(long id)
        {
            var serviceType = await _context.VendorInvestigationServiceType
                .Include(v => v.Country)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(v => v.VendorInvestigationServiceTypeId == id);

            if (serviceType == null) return null;

            serviceType.Currency = CustomExtensions.GetCultureByCountry(serviceType.Country.Code.ToUpper())
                                   .NumberFormat.CurrencySymbol;

            serviceType.InvestigationServiceTypeList = await _context.InvestigationServiceType
                .Where(i => i.InsuranceType == serviceType.InsuranceType)
                .Select(i => new SelectListItem
                {
                    Value = i.InvestigationServiceTypeId.ToString(),
                    Text = i.Name,
                    Selected = i.InvestigationServiceTypeId == serviceType.InvestigationServiceTypeId
                }).ToListAsync();

            return serviceType;
        }

        public async Task<bool> DeleteServiceAsync(long id)
        {
            var service = await _context.VendorInvestigationServiceType
                .FirstOrDefaultAsync(x => x.VendorInvestigationServiceTypeId == id);

            if (service == null) return false;

            _context.VendorInvestigationServiceType.Remove(service);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<VendorInvestigationServiceType> PrepareCreateAsync(long id)
        {
            var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);

            var model = new VendorInvestigationServiceType
            {
                Country = vendor.Country,
                CountryId = vendor.CountryId,
                Vendor = vendor,
                Currency = CustomExtensions.GetCultureByCountry(vendor.Country.Code.ToUpper()).NumberFormat.CurrencySymbol
            };
            return model;
        }
    }
}