using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface IAgencyProfileService
    {
        Task<Vendor> GetAgencyProfileAsync(string userEmail);

        Task<Vendor> GetAgencyForEditAsync(string userEmail);

        Task LoadAgencyMetadataAsync(Vendor model, string userEmail);
    }

    internal class AgencyProfileService : IAgencyProfileService
    {
        private readonly ApplicationDbContext _context;

        public AgencyProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Vendor> GetAgencyProfileAsync(string userEmail)
        {
            var vendorUser = await _context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (vendorUser == null) return null;

            return await _context.Vendor
                .Include(v => v.ratings)
                .Include(v => v.Country)
                .Include(v => v.PinCode)
                .Include(v => v.State)
                .Include(v => v.District)
                .Include(v => v.VendorInvestigationServiceTypes)
                .FirstOrDefaultAsync(m => m.VendorId == vendorUser.VendorId);
        }

        public async Task<Vendor> GetAgencyForEditAsync(string userEmail)
        {
            var vendorUser = await _context.ApplicationUser
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (vendorUser == null) return null;

            var vendor = await _context.Vendor
                .Include(v => v.Country)
                .FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);

            if (vendor != null && vendorUser.IsVendorAdmin)
            {
                vendor.SelectedByCompany = true;
            }

            return vendor;
        }

        public async Task LoadAgencyMetadataAsync(Vendor model, string userEmail)
        {
            var country = await _context.Country.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);

            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;

            var vendorUser = await _context.ApplicationUser.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            model.SelectedByCompany = vendorUser?.IsVendorAdmin ?? false;
        }
    }
}