using Microsoft.FeatureManagement;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IAgencyCreateEditService
    {
        Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string domainAddress, string userEmail, Vendor model, string portal_base_url);
        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, Vendor model, string portal_base_url);
    }
    internal class AgencyCreateEditService : IAgencyCreateEditService
    {
        private readonly ApplicationDbContext context;
        private readonly IFileStorageService fileStorageService;
        private readonly IFeatureManager featureManager;
        private readonly IValidateImageService validateImageService;
        private readonly IPhoneService phoneService;
        private readonly IAgencyService agencyService;

        public AgencyCreateEditService(
            ApplicationDbContext context,
            IFileStorageService fileStorageService,
            IFeatureManager featureManager,
            IValidateImageService validateImageService,
            IPhoneService phoneService,
            IAgencyService agencyService)
        {
            this.context = context;
            this.fileStorageService = fileStorageService;
            this.featureManager = featureManager;
            this.validateImageService = validateImageService;
            this.phoneService = phoneService;
            this.agencyService = agencyService;
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string domainAddress, string userEmail, Vendor model, string portal_base_url)
        {
            var errors = new Dictionary<string, string>();

            validateImageService.ValidateImage(model.Document, errors);

            await ValidatePhoneAsync(model, errors);
            if (errors.Any())
                return (false, errors);
            var result = await agencyService.CreateAgency(model, userEmail, domainAddress, portal_base_url);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error creating Agency." } });
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, Vendor model, string portal_base_url)
        {
            var errors = new Dictionary<string, string>();
            if (model.Document != null && model.Document.Length > 0)
            {
                validateImageService.ValidateImage(model.Document, errors);
            }
            await ValidatePhoneAsync(model, errors);

            if (errors.Any())
                return (false, errors);

            var result = await agencyService.EditAgency(model, userEmail, portal_base_url);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error editing Agency." } });
        }
        private async Task ValidatePhoneAsync(Vendor model, Dictionary<string, string> errors)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                return;

            var country = await context.Country.FindAsync(model.SelectedCountryId);
            if (country == null)
                return;

            if (!phoneService.IsValidMobileNumber(model.PhoneNumber, country.ISDCode.ToString()))
            {
                errors[nameof(BeneficiaryDetail.PhoneNumber)] = "Invalid mobile number";
            }
        }
    }
}
