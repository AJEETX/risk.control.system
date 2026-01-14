using System.Globalization;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ICompanyUserService
    {
        Task<ServiceResult> CreateAsync(ApplicationUser model, string emailSuffix, string performedBy, string portal_base_url);

        Task<ServiceResult> UpdateAsync(long id, ApplicationUser model, string performedBy, string portal_base_url);
    }

    public sealed class CompanyUserService : ICompanyUserService
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExt = new() { ".jpg", ".jpeg", ".png" };
        private static readonly HashSet<string> AllowedMime = new() { "image/jpeg", "image/png" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ISmsService _sms;
        private readonly ILogger<CompanyUserService> _logger;

        public CompanyUserService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ISmsService sms,
            ILogger<CompanyUserService> logger)
        {
            _userManager = userManager;
            _context = context;
            _fileStorage = fileStorage;
            _sms = sms;
            _logger = logger;
        }
        public async Task<ServiceResult> CreateAsync(ApplicationUser model, string emailSuffix, string performedBy, string portal_base_url)
        {
            try
            {
                var result = new ServiceResult();

                if (!RegexHelper.IsMatch(emailSuffix))
                {
                    result.Errors[nameof(model.Email)] = "Invalid email suffix.";
                    return result;
                }

                if (!ValidateProfileImage(model.ProfileImage, result))
                    return result;

                var fullEmail = $"{model.Email.Trim().ToLower(CultureInfo.InvariantCulture)}@{emailSuffix.Trim().ToLower(CultureInfo.InvariantCulture)}";

                if (await _userManager.Users.AnyAsync(u => u.Email == fullEmail && !u.Deleted))
                    return Fail($"User <b>{fullEmail}</b> already exists.");

                await SetProfileImageAsync(model, model.ProfileImage, emailSuffix);

                model.Email = model.UserName = fullEmail;
                model.Password = Applicationsettings.TestingData;
                model.Active = true;
                model.EmailConfirmed = true;

                model.PhoneNumber = model.PhoneNumber.TrimStart('0');
                model.IsClientAdmin = model.Role == AppRoles.COMPANY_ADMIN;
                model.Updated = DateTime.Now;
                model.UpdatedBy = performedBy;
                model.CountryId = model.SelectedCountryId;
                model.StateId = model.SelectedStateId;
                model.DistrictId = model.SelectedDistrictId;
                model.PinCodeId = model.SelectedPincodeId;
                var createResult = await _userManager.CreateAsync(model, model.Password);
                if (!createResult.Succeeded)
                    return Fail($"Failed to create user <b>{fullEmail}</b>.");

                await _userManager.AddToRoleAsync(model, model.Role.ToString());

                var country = await _context.Country.FindAsync(model.CountryId);
                await _sms.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, $"User created\nEmail: {model.Email} \n \r {portal_base_url}");

                return Success($"User <b>{model.Email} </b>created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAsync failed");
                return Fail("Unexpected error while creating user.");
            }
        }
        public async Task<ServiceResult> UpdateAsync(long id, ApplicationUser model, string performedBy, string portal_base_url)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                    return Fail("User not found.");
                var result = new ServiceResult();

                if (model.ProfileImage != null)
                {
                    if (!ValidateProfileImage(model.ProfileImage, result))
                        return result;
                    var domain = user.Email.Split('@')[1];
                    await SetProfileImageAsync(user, model.ProfileImage, domain);
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.CountryId = model.SelectedCountryId;
                user.StateId = model.SelectedStateId;
                user.DistrictId = model.SelectedDistrictId;
                user.PinCodeId = model.SelectedPincodeId;
                user.Addressline = model.Addressline;
                user.PhoneNumber = model.PhoneNumber.TrimStart('0');

                user.Role = model.Role;
                user.IsClientAdmin = user.Role == AppRoles.COMPANY_ADMIN;

                user.Updated = DateTime.Now;
                user.UpdatedBy = performedBy;
                user.SecurityStamp = Guid.NewGuid().ToString();

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return Fail($"Failed to update user <b>{user.Email}</b>.");

                var roles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, user.Role.ToString());
                if (user.Email != performedBy)
                {
                    user.Active = model.Active;

                    if (!user.Active)
                    {
                        await _userManager.SetLockoutEnabledAsync(user, true);
                        await _userManager.SetLockoutEndDateAsync(user, DateTime.MaxValue);
                    }
                    else
                    {
                        await _userManager.SetLockoutEndDateAsync(user, DateTime.Now);
                    }
                }
                var country = await _context.Country.FindAsync(user.CountryId);
                await _sms.DoSendSmsAsync(country.Code, country.ISDCode + model.PhoneNumber, $"User edited\nEmail: {model.Email} \n \r {portal_base_url}");

                return Success($"User <b>{user.Email}</b> updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAsync failed");
                return Fail("Unexpected error while updating user.");
            }
        }

        private static ServiceResult Success(string msg) => new() { Success = true, Message = msg };
        private static ServiceResult Fail(string msg) => new() { Success = false, Message = msg };
        private bool ValidateProfileImage(IFormFile file, ServiceResult result)
        {
            if (file == null || file.Length == 0)
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] =
                    "Profile image is required.";
                return false;
            }

            if (file.Length > MAX_FILE_SIZE)
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] =
                    "Profile image exceeds 5MB.";
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] =
                    "Invalid file type.";
                return false;
            }

            if (!AllowedMime.Contains(file.ContentType))
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] =
                    "Invalid image content type.";
                return false;
            }

            if (!ImageSignatureValidator.HasValidSignature(file))
            {
                result.Errors[nameof(ApplicationUser.ProfileImage)] =
                    "Invalid or corrupted image.";
                return false;
            }

            return true;
        }
        private async Task SetProfileImageAsync(ApplicationUser user, IFormFile file, string domain)
        {
            var (fileName, relativePath) = await _fileStorage.SaveAsync(file, domain, "user");
            user.ProfilePictureUrl = relativePath;
            user.ProfilePictureExtension = Path.GetExtension(fileName);
        }
    }
}
