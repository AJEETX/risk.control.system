using System.Globalization;
using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Company
{
    public interface ICompanyService
    {
        Task<object[]> GetCompanies();

        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, ClientCompany model, string portal_base_url);
    }

    internal class CompanyService : ICompanyService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ISmsService _smsService;
        private readonly ApplicationDbContext _context;
        private readonly IValidateImageService _validateImageService;
        private readonly IPhoneService _phoneService;
        private readonly IFeatureManager _featureManager;
        private readonly IFileStorageService _fileStorageService;
        public CompanyService(
            IWebHostEnvironment env,
            ISmsService smsService,
            ApplicationDbContext context,
            IValidateImageService validateImageService,
            IPhoneService phoneService,
            IFeatureManager featureManager,
            IFileStorageService fileStorageService)
        {
            this._env = env;
            this._smsService = smsService;
            this._context = context;
            this._validateImageService = validateImageService;
            this._phoneService = phoneService;
            this._featureManager = featureManager;
            this._fileStorageService = fileStorageService;
        }

        public async Task<object[]> GetCompanies()
        {
            var companies = _context.ClientCompany.
                 Where(v => !v.Deleted)
                 .Include(v => v.Country)
                 .Include(v => v.PinCode)
                 .Include(v => v.District)
                 .Include(v => v.State).OrderBy(o => o.Name);

            var result =
                companies.Select(u =>
                new
                {
                    Id = u.ClientCompanyId,
                    Document = string.IsNullOrWhiteSpace(u.DocumentUrl) ? Applicationsettings.NO_IMAGE : string.Format("data:image/*;base64,{0}", Convert.ToBase64String(System.IO.File.ReadAllBytes(
                    Path.Combine(_env.ContentRootPath, u.DocumentUrl)))),
                    Domain = u.Email,
                    Name = u.Name,
                    //Code = u.Code,
                    Phone = "(+" + u.Country!.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District!.Name,
                    State = u.State!.Code,
                    CountryCode = u.Country.Code,
                    Country = u.Country.Name,
                    PinCode = $"{u.PinCode!.Name} - {u.PinCode.Code}",
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated ?? u.Created,
                    Active = u.Status!.GetEnumDisplayName(),
                    UpdatedBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();
            companies.ToList().ForEach(u => u.IsUpdated = false);
            await _context.SaveChangesAsync(null, false);
            return result!;
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, ClientCompany model, string portal_base_url)
        {
            var errors = new Dictionary<string, string>();
            if (model.Document != null)
            {
                _validateImageService.ValidateImage(model.Document, errors);
                if (errors.Any())
                    return (false, errors);
            }

            await ValidatePhoneAsync(model, errors);
            if (errors.Any())
                return (false, errors);

            var companyUser = await _context.ApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
            {
                errors[nameof(ApplicationUser.Email)] = "User not found.";
                return (false, errors);
            }
            var company = await _context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

            if (company == null)
            {
                errors[nameof(ClientCompany.Email)] = "Company not found.";
                return (false, errors);
            }
            if (model.Document != null)
            {
                await UpdateDocumentAsync(company, model);
            }
            ApplyCompanyChanges(company, model, userEmail);

            _context.ClientCompany.Update(company);
            await _context.SaveChangesAsync(null, false);

            await SendNotificationAsync(company, model.Email, portal_base_url);

            return (true, null!);
        }

        private async Task ValidatePhoneAsync(ClientCompany model, Dictionary<string, string> errors)
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

        private async Task UpdateDocumentAsync(ClientCompany company, ClientCompany model)
        {
            var (fileName, relativePath) = await _fileStorageService.SaveAsync(model.Document!, model.Email, "user");

            company.DocumentUrl = relativePath;
            company.DocumentImageExtension = Path.GetExtension(fileName);
        }

        private static void ApplyCompanyChanges(ClientCompany company, ClientCompany model, string email)
        {
            company.CountryId = model.SelectedCountryId;
            company.StateId = model.SelectedStateId;
            company.DistrictId = model.SelectedDistrictId;
            company.PinCodeId = model.SelectedPincodeId;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            company.Name = WebUtility.HtmlEncode(textInfo.ToTitleCase(model.Name.ToLower()));

            company.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
            company.Branch = WebUtility.HtmlEncode(model.Branch);
            company.BankName = WebUtility.HtmlEncode(model.BankName);
            company.BankAccountNumber = WebUtility.HtmlEncode(model.BankAccountNumber);
            company.IFSCCode = WebUtility.HtmlEncode(model.IFSCCode!.ToUpper());
            company.Addressline = WebUtility.HtmlEncode(model.Addressline);

            company.Updated = DateTime.UtcNow;
            company.UpdatedBy = email;
        }

        private async Task SendNotificationAsync(ClientCompany company, string email, string portal_base_url)
        {
            string message = $"Company {company.Email} profile edited.\nDomain :{email}\n{portal_base_url}";

            await _smsService.DoSendSmsAsync(company.Country!.Code, company.Country.ISDCode + company.PhoneNumber, message);
        }
    }
}