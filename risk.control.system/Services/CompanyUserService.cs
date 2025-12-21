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
        Task<ServiceResult> CreateAsync(ClientCompanyApplicationUser model, string emailSuffix, string performedBy);

        Task<ServiceResult> UpdateAsync(string id, ClientCompanyApplicationUser model, string performedBy);
    }

    public sealed class CompanyUserService : ICompanyUserService
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExt = new() { ".jpg", ".jpeg", ".png" };
        private static readonly HashSet<string> AllowedMime = new() { "image/jpeg", "image/png" };

        private readonly UserManager<ClientCompanyApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ISmsService _sms;
        private readonly ILogger<CompanyUserService> _logger;

        public CompanyUserService(
            UserManager<ClientCompanyApplicationUser> userManager,
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
        public async Task<ServiceResult> CreateAsync(ClientCompanyApplicationUser model, string emailSuffix, string performedBy)
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

                var fullEmail =
                    $"{model.Email.Trim().ToLower(CultureInfo.InvariantCulture)}@{emailSuffix.Trim().ToLower(CultureInfo.InvariantCulture)}";

                if (await _userManager.Users.AnyAsync(u => u.Email == fullEmail && !u.Deleted))
                    return Fail("User already exists.");

                await SetProfileImageAsync(model, model.ProfileImage, emailSuffix);

                model.Email = model.UserName = fullEmail;
                model.Password = Applicationsettings.TestingData;
                model.Active = true;
                model.EmailConfirmed = true;

                model.PhoneNumber = model.PhoneNumber.TrimStart('0');
                model.Role = (AppRoles)Enum.Parse(typeof(AppRoles), model.UserRole.ToString());
                model.IsClientAdmin = model.UserRole == CompanyRole.COMPANY_ADMIN;
                model.Updated = DateTime.Now;
                model.UpdatedBy = performedBy;

                var createResult = await _userManager.CreateAsync(model, model.Password);
                if (!createResult.Succeeded)
                    return Fail("Failed to create user.");

                await _userManager.AddToRoleAsync(model, model.UserRole.ToString());

                var country = await _context.Country.FindAsync(model.CountryId);
                await _sms.DoSendSmsAsync(
                    country.Code,
                    country.ISDCode + model.PhoneNumber,
                    $"User created\nEmail: {model.Email}");

                return Success("User created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAsync failed");
                return Fail("Unexpected error while creating user.");
            }
        }
        public async Task<ServiceResult> UpdateAsync(string id, ClientCompanyApplicationUser model, string performedBy)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
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
                user.Addressline = model.Addressline;
                user.PhoneNumber = model.PhoneNumber.TrimStart('0');
                user.Active = model.Active;

                user.UserRole = model.UserRole;
                user.Role = (AppRoles)Enum.Parse(typeof(AppRoles), model.UserRole.ToString());
                user.IsClientAdmin = user.UserRole == CompanyRole.COMPANY_ADMIN;

                user.Updated = DateTime.Now;
                user.UpdatedBy = performedBy;
                user.SecurityStamp = Guid.NewGuid().ToString();

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return Fail("Failed to update user.");

                var roles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, user.UserRole.ToString());

                if (!user.Active)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTime.MaxValue);
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTime.Now);
                }

                return Success("User updated successfully.");
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
                result.Errors[nameof(ClientCompanyApplicationUser.ProfileImage)] =
                    "Profile image is required.";
                return false;
            }

            if (file.Length > MAX_FILE_SIZE)
            {
                result.Errors[nameof(ClientCompanyApplicationUser.ProfileImage)] =
                    "Profile image exceeds 5MB.";
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
            {
                result.Errors[nameof(ClientCompanyApplicationUser.ProfileImage)] =
                    "Invalid file type.";
                return false;
            }

            if (!AllowedMime.Contains(file.ContentType))
            {
                result.Errors[nameof(ClientCompanyApplicationUser.ProfileImage)] =
                    "Invalid image content type.";
                return false;
            }

            if (!ImageSignatureValidator.HasValidSignature(file))
            {
                result.Errors[nameof(ClientCompanyApplicationUser.ProfileImage)] =
                    "Invalid or corrupted image.";
                return false;
            }

            return true;
        }

        private async Task SetProfileImageAsync(ClientCompanyApplicationUser user, IFormFile file, string domain)
        {
            var (fileName, relativePath) = await _fileStorage.SaveAsync(file, domain, "user");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            user.ProfilePicture = ms.ToArray();
            user.ProfilePictureUrl = relativePath;
            user.ProfilePictureExtension = Path.GetExtension(fileName);
        }
    }
}
