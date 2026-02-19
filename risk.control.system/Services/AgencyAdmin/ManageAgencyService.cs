using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agency;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface IManageAgencyService
    {
        Task<Vendor> GetVendorForEditAsync(string userEmail);

        Task<Vendor> GetVendorAsync(string userEmail, long id);

        Task<Vendor> GetVendorDetailAsync(long id);

        Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string domainAddress, string userEmail, Vendor model, string baseUrl);

        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, Vendor model, string baseUrl);

        Task<(bool Success, string Message)> SoftDeleteAgencyAsync(long id, string performedBy);

        Task LoadModel(Vendor model);
    }

    internal class ManageAgencyService : IManageAgencyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager _featureManager;
        private readonly IValidateImageService _validateImageService;
        private readonly IAgencyCaseLoadService _agencyCaseLoadService;
        private readonly IPhoneService _phoneService;
        private readonly IAgencyService _agencyService;

        public ManageAgencyService(
            ApplicationDbContext context,
            IFeatureManager featureManager,
            IValidateImageService validateImageService,
            IAgencyCaseLoadService agencyCaseLoadService,
            IPhoneService phoneService,
            IAgencyService agencyService)
        {
            _context = context;
            _featureManager = featureManager;
            _validateImageService = validateImageService;
            _agencyCaseLoadService = agencyCaseLoadService;
            _phoneService = phoneService;
            _agencyService = agencyService;
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string domainAddress, string userEmail, Vendor model, string baseUrl)
        {
            var errors = new Dictionary<string, string>();

            _validateImageService.ValidateImage(model.Document, errors);

            await ValidatePhoneAsync(model, errors);
            if (errors.Any())
                return (false, errors);
            var result = await _agencyService.CreateAgency(model, userEmail, domainAddress, baseUrl);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error creating Agency." } });
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, Vendor model, string baseUrl)
        {
            var errors = new Dictionary<string, string>();
            if (model.Document != null && model.Document.Length > 0)
            {
                _validateImageService.ValidateImage(model.Document, errors);
            }
            await ValidatePhoneAsync(model, errors);

            if (errors.Any())
                return (false, errors);

            var result = await _agencyService.EditAgency(model, userEmail, baseUrl);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error editing Agency." } });
        }

        public async Task<Vendor> GetVendorAsync(string userEmail, long id)
        {
            var vendor = await _context.Vendor.AsNoTracking().Include(v => v.Country).FirstOrDefaultAsync(v => v.VendorId == id);
            vendor.SelectedByCompany = await _context.ApplicationUser.AsNoTracking().AnyAsync(u => u.Email.ToLower() == userEmail.ToLower() && u.IsSuperAdmin);

            return vendor;
        }

        public async Task<Vendor> GetVendorDetailAsync(long id)
        {
            var vendor = await _context.Vendor
                    .Include(v => v.ratings)
                    .Include(v => v.Country)
                    .Include(v => v.PinCode)
                    .Include(v => v.State)
                    .Include(v => v.District)
                    .Include(v => v.VendorInvestigationServiceTypes)
                    .FirstOrDefaultAsync(m => m.VendorId == id);

            var approvedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.APPROVED_BY_ASSESSOR;
            var rejectedStatus = CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REJECTED_BY_ASSESSOR;

            var vendorAllCasesCount = await _context.Investigations.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted &&
                      (c.SubStatus == approvedStatus ||
                      c.SubStatus == rejectedStatus));

            var vendorUserCount = await _context.ApplicationUser.CountAsync(c => c.VendorId == vendor.VendorId && !c.Deleted);

            // HACKY
            var currentCases = await _agencyCaseLoadService.GetAgencyIdsLoad(new List<long> { vendor.VendorId });
            vendor.SelectedCountryId = vendorUserCount;
            vendor.SelectedStateId = currentCases.FirstOrDefault().CaseCount;
            vendor.SelectedDistrictId = vendorAllCasesCount;
            return vendor;
        }

        public async Task<Vendor> GetVendorForEditAsync(string userEmail)
        {
            var companyUser = await _context.ApplicationUser.Include(c => c.Country).Include(c => c.ClientCompany).FirstOrDefaultAsync(c => c.Email == userEmail);
            var vendor = new Vendor
            {
                CountryId = companyUser.ClientCompany.CountryId,
                Country = companyUser.ClientCompany.Country,
                SelectedCountryId = companyUser.ClientCompany.CountryId.Value
            };
            return vendor;
        }

        public async Task<(bool Success, string Message)> SoftDeleteAgencyAsync(long id, string performedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vendor = await _context.Vendor.FindAsync(id);
                if (vendor == null)
                    return (false, "Agency not found.");

                // 1. Soft delete all users associated with this agency
                var vendorUsers = await _context.ApplicationUser
                    .Where(v => v.VendorId == id && !v.Deleted)
                    .ToListAsync();

                foreach (var user in vendorUsers)
                {
                    user.Updated = DateTime.UtcNow;
                    user.UpdatedBy = performedBy;
                    user.Deleted = true;
                }

                // 2. Soft delete the agency itself
                vendor.Updated = DateTime.UtcNow;
                vendor.UpdatedBy = performedBy;
                vendor.Deleted = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Agency {vendor.Email} and its users deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "Error deleting agency. Please try again.");
            }
        }

        public async Task LoadModel(Vendor model)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
            model.Country = country;
            model.CountryId = model.SelectedCountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }

        private async Task ValidatePhoneAsync(Vendor model, Dictionary<string, string> errors)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                return;

            var country = await _context.Country.FindAsync(model.SelectedCountryId);
            if (country == null)
                return;

            if (!_phoneService.IsValidMobileNumber(model.PhoneNumber, country.ISDCode.ToString()))
            {
                errors[nameof(BeneficiaryDetail.PhoneNumber)] = "Invalid mobile number";
            }
        }
    }
}