using System.Net;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Creator
{
    public interface ICustomerService
    {
        Task<CustomerDetail> GetCreateViewModelAsync(long investigationId, string userEmail);

        Task PrepareMetadataAsync(CustomerDetail model, string userEmail);

        Task<CustomerDetail> GetCustomerDetailAsync(long id, long countryId);

        Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string userEmail, CustomerDetail model);

        Task<CustomerDetail> GetEditViewModelAsync(long investigationId, string userEmail);

        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, CustomerDetail model);
    }

    internal class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager _featureManager;
        private readonly IAddInvestigationService _addInvestigationService;
        private readonly IPhoneService _phoneService;
        private readonly IValidateImageService _validateImageService;

        public CustomerService(
            ApplicationDbContext context,
            IFeatureManager featureManager,
            IAddInvestigationService addInvestigationService,
            IPhoneService phoneService,
            IValidateImageService validateImageService)
        {
            this._context = context;
            this._featureManager = featureManager;
            this._addInvestigationService = addInvestigationService;
            this._phoneService = phoneService;
            this._validateImageService = validateImageService;
        }

        public async Task<CustomerDetail> GetCreateViewModelAsync(long investigationId, string userEmail)
        {
            var user = await GetUserWithCompanyAsync(userEmail);
            CustomerDetail model;

            if (user.ClientCompany.HasSampleData)
            {
                // Assuming this method exists or you are moving it here
                model = await GetCustomerDetailAsync(investigationId, user.ClientCompany.CountryId.Value);
            }
            else
            {
                model = new CustomerDetail
                {
                    InvestigationTaskId = investigationId,
                    Country = user.ClientCompany.Country,
                    CountryId = user.ClientCompany.CountryId
                };
            }

            await PrepareMetadataAsync(model, user);
            return model;
        }

        private IEnumerable<SelectListItem> GetEnumSelectList<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>()
                .Select(e => new SelectListItem { Text = e.ToString(), Value = e.ToString() });
        }

        public async Task PrepareMetadataAsync(CustomerDetail model, string userEmail)
        {
            var user = await GetUserWithCompanyAsync(userEmail);
            await PrepareMetadataAsync(model, user);
        }

        private async Task PrepareMetadataAsync(CustomerDetail model, ApplicationUser user)
        {
            // Handle geographical syncing if this is a post-back
            if (model.SelectedCountryId > 0)
            {
                model.Country = await _context.Country.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CountryId == model.SelectedCountryId);
                model.CountryId = model.Country?.CountryId ?? model.CountryId;
            }

            model.StateId = model.SelectedStateId > 0 ? model.SelectedStateId : model.StateId;
            model.DistrictId = model.SelectedDistrictId > 0 ? model.SelectedDistrictId : model.DistrictId;
            model.PinCodeId = model.SelectedPincodeId > 0 ? model.SelectedPincodeId : model.PinCodeId;

            // Set Currency
            var countryCode = user.ClientCompany.Country.Code.ToUpper();
            model.CurrencySymbol = CustomExtensions.GetCultureByCountry(countryCode).NumberFormat.CurrencySymbol;

            // Populate Enum Dropdowns
            model.GenderList = new SelectList(Enum.GetValues(typeof(Gender)), model.Gender);
            model.IncomeList = new SelectList(Enum.GetValues(typeof(Income)), model.Income);
            model.EducationList = new SelectList(Enum.GetValues(typeof(Education)), model.Education);
            model.OccupationList = new SelectList(Enum.GetValues(typeof(Occupation)), model.Occupation);
        }

        private async Task<ApplicationUser> GetUserWithCompanyAsync(string email)
        {
            return await _context.ApplicationUser.AsNoTracking()
                .Include(c => c.ClientCompany).ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == email)
                ?? throw new KeyNotFoundException("User not found");
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string userEmail, CustomerDetail model)
        {
            var errors = new Dictionary<string, string>();

            _validateImageService.ValidateImage(model.ProfileImage, errors);
            await ValidatePhoneAsync(model, errors);

            if (errors.Any())
                return (false, errors);

            Sanitize(model);

            var result = await _addInvestigationService.CreateCustomer(userEmail, model);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error creating customer." } });
        }

        private async Task ValidatePhoneAsync(CustomerDetail model, Dictionary<string, string> errors)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                return;

            var country = await _context.Country.FindAsync(model.SelectedCountryId);
            if (country == null)
                return;

            if (!_phoneService.IsValidMobileNumber(model.PhoneNumber, country.ISDCode.ToString()))
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
            var pinCode = await _context.PinCode.Include(s => s.Country).FirstOrDefaultAsync(s => s.Country.CountryId == countryId);
            var customerDetail = new CustomerDetail
            {
                InvestigationTaskId = id,
                Addressline = "12 Main Road",
                PhoneNumber = pinCode.Country.Code.ToLower() == "au" ? Applicationsettings.SAMPLE_MOBILE_AUSTRALIA : Applicationsettings.SAMPLE_MOBILE_INDIA,
                DateOfBirth = DateTime.UtcNow.AddYears(-30).AddDays(20),
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

        public async Task<CustomerDetail> GetEditViewModelAsync(long investigationId, string userEmail)
        {
            // 1. Fetch the existing record with geographic relations
            var model = await _context.CustomerDetail.AsNoTracking()
                .Include(c => c.PinCode)
                .Include(c => c.District)
                .Include(c => c.State)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(i => i.InvestigationTaskId == investigationId)
                ?? throw new KeyNotFoundException("Customer Not Found");

            // 2. Reuse the shared metadata preparation logic
            await PrepareMetadataAsync(model, userEmail);

            return model;
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, CustomerDetail model)
        {
            var errors = new Dictionary<string, string>();
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                _validateImageService.ValidateImage(model.ProfileImage, errors);
            }
            await ValidatePhoneAsync(model, errors);

            if (errors.Any())
                return (false, errors);

            Sanitize(model);

            var result = await _addInvestigationService.EditCustomer(userEmail, model);
            return result
                ? (true, errors)
                : (false, new Dictionary<string, string>
                    { { string.Empty, "Error editing  customer." } });
        }
    }
}