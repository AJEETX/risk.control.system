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
    public interface ICompanyService
    {
        Task<object[]> GetCompanies();
        Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, ClientCompany model, string portal_base_url);

    }
    internal class CompanyService : ICompanyService
    {
        private readonly IWebHostEnvironment env;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext context;
        private readonly IValidateImageService validateImageService;
        private readonly IPhoneService phoneService;
        private readonly IFeatureManager featureManager;
        private readonly IFileStorageService fileStorageService;

        public CompanyService(
            IWebHostEnvironment env,
            ISmsService smsService,
            ApplicationDbContext context,
            IValidateImageService validateImageService,
            IPhoneService phoneService,
            IFeatureManager featureManager,
            IFileStorageService fileStorageService)
        {
            this.env = env;
            this.smsService = smsService;
            this.context = context;
            this.validateImageService = validateImageService;
            this.phoneService = phoneService;
            this.featureManager = featureManager;
            this.fileStorageService = fileStorageService;
        }
        public async Task<object[]> GetCompanies()
        {
            var companies = context.ClientCompany.
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
                    Path.Combine(env.ContentRootPath, u.DocumentUrl)))),
                    Domain = $"<a href='/ClientCompany/Details?Id={u.ClientCompanyId}'>" + u.Email + "</a>",
                    Name = u.Name,
                    //Code = u.Code,
                    Phone = "(+" + u.Country.ISDCode + ") " + u.PhoneNumber,
                    Address = u.Addressline,
                    District = u.District.Name,
                    State = u.State.Code,
                    Country = u.Country.Code,
                    Flag = "/flags/" + u.Country.Code.ToLower() + ".png",
                    Updated = u.Updated.HasValue ? u.Updated.Value.ToString("dd-MM-yyyy") : u.Created.ToString("dd-MM-yyyy"),
                    Active = u.Status.GetEnumDisplayName(),
                    UpdatedBy = u.UpdatedBy,
                    IsUpdated = u.IsUpdated,
                    LastModified = u.Updated
                })?.ToArray();
            companies.ToList().ForEach(u => u.IsUpdated = false);
            await context.SaveChangesAsync(null, false);
            return result;
        }
        public async Task<(bool Success, Dictionary<string, string> Errors)> EditAsync(string userEmail, ClientCompany model, string portal_base_url)
        {
            var errors = new Dictionary<string, string>();
            if (model.Document != null)
            {
                validateImageService.ValidateImage(model.Document, errors);
                if (errors.Any())
                    return (false, errors);
            }

            await ValidatePhoneAsync(model, errors);
            if (errors.Any())
                return (false, errors);

            var companyUser = await context.ClientCompanyApplicationUser.FirstOrDefaultAsync(c => c.Email == userEmail);

            if (companyUser == null)
            {
                errors[nameof(ClientCompanyApplicationUser.Email)] = "User not found.";
                return (false, errors);
            }
            var company = await context.ClientCompany.Include(c => c.Country).FirstOrDefaultAsync(c => c.ClientCompanyId == companyUser.ClientCompanyId);

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

            context.ClientCompany.Update(company);
            await context.SaveChangesAsync();

            await SendNotificationAsync(company, model.Email, portal_base_url);

            return (true, null);
        }

        private async Task ValidatePhoneAsync(ClientCompany model, Dictionary<string, string> errors)
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

        private async Task UpdateDocumentAsync(ClientCompany company, ClientCompany model)
        {
            var (fileName, relativePath) = await fileStorageService.SaveAsync(model.Document, model.Email, "user");

            company.DocumentUrl = relativePath;
            company.DocumentImageExtension = Path.GetExtension(fileName);
        }

        private static void ApplyCompanyChanges(ClientCompany company, ClientCompany model, string email)
        {
            company.CountryId = model.SelectedCountryId;
            company.StateId = model.SelectedStateId;
            company.DistrictId = model.SelectedDistrictId;
            company.PinCodeId = model.SelectedPincodeId;

            company.Name = WebUtility.HtmlEncode(model.Name);
            company.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
            company.Branch = WebUtility.HtmlEncode(model.Branch);
            company.BankName = WebUtility.HtmlEncode(model.BankName);
            company.BankAccountNumber = WebUtility.HtmlEncode(model.BankAccountNumber);
            company.IFSCCode = WebUtility.HtmlEncode(model.IFSCCode.ToUpper());
            company.Addressline = WebUtility.HtmlEncode(model.Addressline);

            company.Updated = DateTime.UtcNow;
            company.UpdatedBy = email;
        }

        private async Task SendNotificationAsync(ClientCompany company, string email, string portal_base_url)
        {
            string message = $"Company edited.\nDomain : {email}\n{portal_base_url}";

            await smsService.DoSendSmsAsync(company.Country.Code, company.Country.ISDCode + company.PhoneNumber, message);
        }
    }
}
