using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IBeneficiaryCreateEditService
    {
        Task<BeneficiaryDetail> GetBeneficiaryDetailAsync(long id, long countryId);
        Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string userEmail, BeneficiaryDetail model);
        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, BeneficiaryDetail model);

    }
    public class BeneficiaryCreateEditService : IBeneficiaryCreateEditService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExt = new() { ".jpg", ".jpeg", ".png" };
        private static readonly HashSet<string> AllowedMime = new() { "image/jpeg", "image/png" };

        private readonly ApplicationDbContext context;
        private readonly IFeatureManager featureManager;
        private readonly IAddInvestigationService addInvestigationService;
        private readonly IPhoneService phoneService;
        private readonly IValidateImageService validateImageService;

        public BeneficiaryCreateEditService(
            ApplicationDbContext context,
            IFeatureManager featureManager,
            IAddInvestigationService addInvestigationService,
            IPhoneService phoneService,
            IValidateImageService validateImageService)
        {
            this.context = context;
            this.featureManager = featureManager;
            this.addInvestigationService = addInvestigationService;
            this.phoneService = phoneService;
            this.validateImageService = validateImageService;
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string userEmail, BeneficiaryDetail model)
        {
            var errors = new Dictionary<string, string>();

            validateImageService.ValidateImage(model.ProfileImage, errors);
            await ValidatePhoneAsync(model, errors);

            if (errors.Any())
                return (false, errors);

            Sanitize(model);

            var result = await addInvestigationService.CreateBeneficiary(userEmail, model);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error creating beneficiary." } });
        }

        private async Task ValidatePhoneAsync(BeneficiaryDetail model, Dictionary<string, string> errors)
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

        private static void Sanitize(BeneficiaryDetail model)
        {
            model.Name = WebUtility.HtmlEncode(model.Name);
            model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber);
        }
        public async Task<BeneficiaryDetail> GetBeneficiaryDetailAsync(long id, long countryId)
        {
            var beneRelation = await context.BeneficiaryRelation.FirstOrDefaultAsync();
            var pinCode = await context.PinCode.Include(s => s.Country).OrderBy(p => p.StateId).LastOrDefaultAsync(s => s.Country.CountryId == countryId);

            var model = new BeneficiaryDetail
            {
                InvestigationTaskId = id,
                Addressline = "10 Main Road",
                DateOfBirth = DateTime.Now.AddYears(-25).AddMonths(3),
                Income = Income.MEDIUM_INCOME,
                Name = NameGenerator.GenerateName(),
                BeneficiaryRelationId = beneRelation.BeneficiaryRelationId,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                SelectedCountryId = pinCode.CountryId.GetValueOrDefault(),
                StateId = pinCode.StateId,
                SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                DistrictId = pinCode.DistrictId,
                SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault(),
                PinCodeId = pinCode.PinCodeId,
                SelectedPincodeId = pinCode.PinCodeId,
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
            };
            return model;
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, BeneficiaryDetail model)
        {
            var errors = new Dictionary<string, string>();
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                validateImageService.ValidateImage(model.ProfileImage, errors);
            }
            await ValidatePhoneAsync(model, errors);

            if (errors.Any())
                return (false, errors);

            Sanitize(model);

            var result = await addInvestigationService.EditBeneficiary(userEmail, model);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error editing beneficiary." } });
        }
    }
}
