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
    public interface IBeneficiaryService
    {
        Task<BeneficiaryDetail> GetViewModelAsync(long investigationId, string userEmail);

        Task PrepareFailedPostModelAsync(BeneficiaryDetail model, string userEmail);

        Task<BeneficiaryDetail> GetBeneficiaryDetailAsync(long id, long countryId);

        Task<(bool Success, Dictionary<string, string> Errors)> CreateAsync(string userEmail, BeneficiaryDetail model);

        Task<BeneficiaryDetail> GetEditViewModelAsync(long investigationId, string userEmail);

        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, BeneficiaryDetail model);
    }

    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager featureManager;
        private readonly IAddInvestigationService addInvestigationService;
        private readonly IPhoneService phoneService;
        private readonly IValidateImageService validateImageService;

        public BeneficiaryService(
            ApplicationDbContext context,
            IFeatureManager featureManager,
            IAddInvestigationService addInvestigationService,
            IPhoneService phoneService,
            IValidateImageService validateImageService)
        {
            this._context = context;
            this.featureManager = featureManager;
            this.addInvestigationService = addInvestigationService;
            this.phoneService = phoneService;
            this.validateImageService = validateImageService;
        }

        public async Task<BeneficiaryDetail> GetViewModelAsync(long investigationId, string userEmail)
        {
            var user = await _context.ApplicationUser
                .AsNoTracking()
                .Include(c => c.ClientCompany).ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(c => c.Email == userEmail)
                ?? throw new KeyNotFoundException("User not found");

            BeneficiaryDetail model;

            if (user.ClientCompany.HasSampleData)
            {
                // Fetch existing detail if sample data is enabled
                model = await GetBeneficiaryDetailAsync(investigationId, user.ClientCompany.CountryId.Value);
            }
            else
            {
                // Initialize fresh model
                model = new BeneficiaryDetail
                {
                    InvestigationTaskId = investigationId,
                    Country = user.ClientCompany.Country,
                    CountryId = user.ClientCompany.CountryId
                };
            }

            await PopulateMetadataAsync(model, user);
            return model;
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

        public async Task<BeneficiaryDetail> GetBeneficiaryDetailAsync(long id, long countryId)
        {
            var beneRelation = await _context.BeneficiaryRelation.FirstOrDefaultAsync();
            var pinCode = await _context.PinCode.Include(s => s.Country).OrderBy(p => p.StateId).LastOrDefaultAsync(s => s.Country.CountryId == countryId);

            var model = new BeneficiaryDetail
            {
                InvestigationTaskId = id,
                Addressline = "10 Main Road",
                DateOfBirth = DateTime.UtcNow.AddYears(-25).AddMonths(3),
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

        public async Task<BeneficiaryDetail> GetEditViewModelAsync(long investigationId, string userEmail)
        {
            var model = await _context.BeneficiaryDetail
                .Include(v => v.PinCode)
                .Include(v => v.District)
                .Include(v => v.State)
                .Include(v => v.Country)
                .Include(v => v.BeneficiaryRelation)
                .FirstOrDefaultAsync(v => v.InvestigationTaskId == investigationId)
                ?? throw new KeyNotFoundException($"Beneficiary for Task {investigationId} not found.");

            var user = await _context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

            await PopulateMetadataAsync(model, user);

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

        public async Task PrepareFailedPostModelAsync(BeneficiaryDetail model, string userEmail)
        {
            var user = await _context.ApplicationUser.AsNoTracking().Include(c => c.ClientCompany).ThenInclude(c => c.Country).FirstOrDefaultAsync(c => c.Email == userEmail);

            model.CountryId = model.SelectedCountryId > 0 ? model.SelectedCountryId : model.CountryId;
            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
            model.Country = await _context.Country.AsNoTracking().FirstOrDefaultAsync(c => c.CountryId == model.CountryId);

            await PopulateMetadataAsync(model, user);
        }

        private async Task PopulateMetadataAsync(BeneficiaryDetail model, ApplicationUser user)
        {
            // 1. Set Localized Currency
            model.CurrencySymbol = CustomExtensions.GetCultureByCountry(user.ClientCompany.Country.Code.ToUpper()).NumberFormat.CurrencySymbol;

            // 2. Load Relations from DB
            model.BeneficiaryRelations = await _context.BeneficiaryRelation.AsNoTracking().Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.BeneficiaryRelationId.ToString(),
                Selected = r.BeneficiaryRelationId == model.BeneficiaryRelationId
            }).ToListAsync();

            // 3. Load Income Enum
            model.Incomes = Enum.GetValues(typeof(Income)).Cast<Income>()
                .Select(i => new SelectListItem
                {
                    Text = i.ToString(),
                    Value = i.ToString(),
                    Selected = i == model.Income
                });
        }

        private async Task ValidatePhoneAsync(BeneficiaryDetail model, Dictionary<string, string> errors)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.VALIDATE_PHONE))
                return;

            var country = await _context.Country.FindAsync(model.SelectedCountryId);
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
    }
}