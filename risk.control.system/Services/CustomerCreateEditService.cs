using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICustomerCreateEditService
    {
        Task<CustomerDetail> GetCustomerDetailAsync(long id, long countryId);
        Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string userEmail, CustomerDetail model);
        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, CustomerDetail model);

    }
    internal class CustomerCreateEditService : ICustomerCreateEditService
    {
        private readonly ApplicationDbContext context;
        private readonly IFeatureManager featureManager;
        private readonly IAddInvestigationService addInvestigationService;
        private readonly IPhoneService phoneService;
        private readonly IValidateImageService validateImageService;

        public CustomerCreateEditService(
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

        public async Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string userEmail, CustomerDetail model)
        {
            var errors = new Dictionary<string, string>();

            validateImageService.ValidateImage(model.ProfileImage, errors);
            await ValidatePhoneAsync(model, errors);

            if (errors.Any())
                return (false, errors);

            Sanitize(model);

            var result = await addInvestigationService.CreateCustomer(userEmail, model);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error creating customer." } });
        }

        private async Task ValidatePhoneAsync(CustomerDetail model, Dictionary<string, string> errors)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                return;

            var country = await context.Country.FindAsync(model.SelectedCountryId);
            if (country == null)
                return;

            if (!phoneService.IsValidMobileNumber(model.PhoneNumber, country.ISDCode.ToString()))
            {
                errors[nameof(CustomerDetail.PhoneNumber)] = "Invalid mobile number";
            }
        }

        private static void Sanitize(CustomerDetail model)
        {
            model.Name = WebUtility.HtmlEncode(model.Name);
            model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber);
        }

        public async Task<CustomerDetail> GetCustomerDetailAsync(long id, long countryId)
        {
            var pinCode = await context.PinCode.Include(s => s.Country).FirstOrDefaultAsync(s => s.Country.CountryId == countryId);
            var customerDetail = new CustomerDetail
            {
                InvestigationTaskId = id,
                Addressline = "12 Main Road",
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                DateOfBirth = DateTime.Now.AddYears(-30).AddDays(20),
                Education = Education.PROFESSIONAL,
                Income = Income.UPPER_INCOME,
                Name = NameGenerator.GenerateName(),
                Occupation = Occupation.SELF_EMPLOYED,
                Country = pinCode.Country,
                CountryId = pinCode.CountryId,
                SelectedCountryId = pinCode.CountryId.GetValueOrDefault(),
                StateId = pinCode.StateId,
                SelectedStateId = pinCode.StateId.GetValueOrDefault(),
                DistrictId = pinCode.DistrictId,
                SelectedDistrictId = pinCode.DistrictId.GetValueOrDefault(),
                PinCodeId = pinCode.PinCodeId,
                SelectedPincodeId = pinCode.PinCodeId,
                Gender = Gender.MALE,
            };
            return customerDetail;
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, CustomerDetail model)
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

            var result = await addInvestigationService.EditCustomer(userEmail, model);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error editing  customer." } });
        }
    }
}
