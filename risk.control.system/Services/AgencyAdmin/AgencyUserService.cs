using System.Net;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface IAgencyUserService
    {
        Task<ApplicationUser> GetUserAsync(long id);

        Task<ApplicationUser> GetChangePasswordUserAsync(string userEmail);

        Task<ServiceResult> UpdateUserAsync(string id, ApplicationUser model, string updatedBy, string portal_base_url);

        Task LoadModel(ApplicationUser model, string currentUserEmail);
    }

    internal class AgencyUserService : IAgencyUserService
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMime = new[] { "image/jpeg", "image/png" };
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileStorageService _fileStorageService;
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;

        public AgencyUserService(
            UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorageService,
            ApplicationDbContext context,
            ISmsService smsService)
        {
            _userManager = userManager;
            _fileStorageService = fileStorageService;
            _context = context;
            _smsService = smsService;
        }

        public async Task<ServiceResult> UpdateUserAsync(string id, ApplicationUser model, string updatedBy, string portal_base_url)
        {
            var result = new ServiceResult();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                result.Message = "User not found";
                return result;
            }

            if (model.ProfileImage != null)
            {
                var imageValidation = ValidateProfileImage(model.ProfileImage);
                if (!imageValidation.Success)
                    return imageValidation;

                await ProcessProfileImageAsync(model);
            }

            MapUserFields(user, model, updatedBy);

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    result.Errors.TryAdd(string.Empty, error.Description);
                }

                result.Message = "Failed to update user";
                return result;
            }

            await SendSmsNotificationAsync(user, portal_base_url);

            result.Success = true;
            return result;
        }

        public async Task LoadModel(ApplicationUser model, string currentUserEmail)
        {
            var vendorUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(c => c.Email == currentUserEmail);
            var vendor = await _context.Vendor.AsNoTracking().Include(c => c.Country).FirstOrDefaultAsync(v => v.VendorId == vendorUser.VendorId);
            model.Vendor = vendor;
            model.Country = vendor.Country;
            model.CountryId = vendor.CountryId;

            model.StateId = model.SelectedStateId;
            model.DistrictId = model.SelectedDistrictId;
            model.PinCodeId = model.SelectedPincodeId;
        }

        public async Task<ApplicationUser> GetUserAsync(long id)
        {
            var agencyUser = await _context.ApplicationUser.AsNoTracking().Include(u => u.ClientCompany).Include(c => c.Country).FirstOrDefaultAsync(u => u.Id == id);
            return agencyUser;
        }

        public async Task<ApplicationUser> GetChangePasswordUserAsync(string userEmail)
        {
            var agencyUser = await _context.ApplicationUser.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);
            return agencyUser;
        }

        private static ServiceResult ValidateProfileImage(IFormFile file)
        {
            if (file.Length > MAX_FILE_SIZE)
                return Error(nameof(ApplicationUser.ProfileImage),
                    "Document image size exceeds the maximum limit (5MB).");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
                return Error(nameof(ApplicationUser.ProfileImage),
                    "Invalid document image type.");

            if (!AllowedMime.Contains(file.ContentType))
                return Error(nameof(ApplicationUser.ProfileImage),
                    "Invalid document image content type.");

            if (!ImageSignatureValidator.HasValidSignature(file))
                return Error(nameof(ApplicationUser.ProfileImage),
                    "Invalid or corrupted document image.");

            return new ServiceResult { Success = true };
        }

        private async Task ProcessProfileImageAsync(ApplicationUser model)
        {
            var domain = WebUtility.HtmlEncode(model.Email.Split('@')[1]);
            var (fileName, relativePath) = await _fileStorageService.SaveAsync(model.ProfileImage, domain, "user");

            model.ProfilePictureUrl = relativePath;
            model.ProfilePictureExtension = Path.GetExtension(fileName);
        }

        private static void MapUserFields(ApplicationUser user, ApplicationUser model, string updatedBy)
        {
            user.Addressline = WebUtility.HtmlEncode(model.Addressline);
            user.FirstName = WebUtility.HtmlEncode(model.FirstName);
            user.LastName = WebUtility.HtmlEncode(model.LastName);
            user.ProfilePictureUrl = model.ProfilePictureUrl ?? user.ProfilePictureUrl;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.EmailConfirmed = true;
            user.CountryId = model.SelectedCountryId;
            user.StateId = model.SelectedStateId;
            user.PinCodeId = model.SelectedPincodeId;
            user.DistrictId = model.SelectedDistrictId;
            user.IsUpdated = true;
            user.Updated = DateTime.UtcNow;
            user.Comments = WebUtility.HtmlEncode(model.Comments);
            user.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber.TrimStart('0'));
            user.UpdatedBy = updatedBy;
            user.SecurityStamp = DateTime.UtcNow.ToString();

            if (!string.IsNullOrWhiteSpace(model.Password))
                user.Password = model.Password;
        }

        private async Task SendSmsNotificationAsync(ApplicationUser user, string portal_base_url)
        {
            var country = await _context.Country.FirstOrDefaultAsync(c => c.CountryId == user.CountryId);
            if (country == null) return;

            await _smsService.DoSendSmsAsync(
                country.Code,
                country.ISDCode + user.PhoneNumber,
                $"Agency user edited.\nEmail: {user.Email}\n{portal_base_url}");
        }

        private static ServiceResult Error(string key, string message)
        {
            var result = new ServiceResult();
            result.Errors.Add(key, message);
            return result;
        }
    }
}