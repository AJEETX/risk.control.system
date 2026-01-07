using System.Net;
using System.Text.RegularExpressions;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IAgencyUserCreateEditService
    {
        Task<(bool Success, string Message, Dictionary<string, string> Errors)> CreateVendorUserAsync(CreateVendorUserRequest request, ModelStateDictionary modelState, string portal_base_url);
        Task<(bool Success, string Message, Dictionary<string, string> Errors)> EditVendorUserAsync(EditVendorUserRequest request, ModelStateDictionary modelState, string portal_base_url);


    }
    public class AgencyUserCreateEditService : IAgencyUserCreateEditService
    {
        private readonly IValidateImageService validateImageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly INotyfService _notify;
        private readonly ISmsService _sms;
        private readonly ICustomApiClient _geoClient;
        private readonly ITinyUrlService urlService;

        public AgencyUserCreateEditService(
            IValidateImageService validateImageService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            INotyfService notify,
            ISmsService sms,
            ICustomApiClient geoClient,
            ITinyUrlService urlService)
        {
            this.validateImageService = validateImageService;
            _userManager = userManager;
            _context = context;
            _fileStorage = fileStorage;
            _notify = notify;
            _sms = sms;
            _geoClient = geoClient;
            this.urlService = urlService;
        }

        public async Task<(bool Success, string Message, Dictionary<string, string> Errors)> CreateVendorUserAsync(CreateVendorUserRequest request, ModelStateDictionary modelState, string portal_base_url)
        {
            var errors = new Dictionary<string, string>();

            if (!RegexHelper.IsMatch(request.EmailSuffix))
            {
                modelState.AddModelError("Email", "Invalid email address.");
                errors.Add("Email", "Invalid email address.");
                return (false, "Invalid email address.", errors);
            }

            var model = request.User;

            var email = $"{model.Email.Trim().ToLowerInvariant()}@{request.EmailSuffix.Trim().ToLowerInvariant()}";

            if (await _userManager.Users.AnyAsync(u => u.Email == email && !u.Deleted))
            {
                modelState.AddModelError("Email", $"User with email {email} already exists.");
                errors.Add("Email", $"User with email {email} already exists.");
                return (false, $"User with email {email} already exists.", errors);
            }
            validateImageService.ValidateImage(model.ProfileImage, errors);
            if (errors.Any())
            {
                return (false, "Invalid profile image.", errors);
            }
            await SaveProfileImageAsync(model, request.EmailSuffix);

            PopulateUserEntity(model, email, request.CreatedBy);
            await UpdateGeoLocationAsync(model);

            using var tx = await _context.Database.BeginTransactionAsync();

            var tempPassword = Applicationsettings.TestingData;
            var result = await _userManager.CreateAsync(model, tempPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    errors.Add("CreateUser", error.Description);
                }
                return (false, "Error creating user.", errors);
            }
            await _userManager.AddToRoleAsync(model, model.Role.ToString());

            await HandleLockAndNotificationsAsync(model, portal_base_url);

            await tx.CommitAsync();

            return (true, $"User {email} created successfully.", errors);
        }
        public async Task<(bool Success, string Message, Dictionary<string, string> Errors)> EditVendorUserAsync(EditVendorUserRequest request, ModelStateDictionary modelState, string portal_base_url)
        {
            var input = request.Model;
            var errors = new Dictionary<string, string>();

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                errors.Add("Email", "User not found");
                return (false, "User not found", errors);
            }

            if (input.ProfileImage != null && input.ProfileImage.Length > 0)
            {
                validateImageService.ValidateImage(input.ProfileImage, errors);
            }
            if (errors.Any())
            {
                return (false, "Invalid profile image.", errors);
            }
            if (input.ProfileImage != null && input.ProfileImage.Length > 0)
            {
                var suffix = user.Email.Split('@').Last();
                await SaveProfileImageAsync(input, suffix);
            }
            UpdateUserFields(input, user, request.UpdatedBy);

            await UpdateGeoLocationAsync(user);

            using var tx = await _context.Database.BeginTransactionAsync();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    errors.Add("EditUser", error.Description);
                }
                return (false, "Error updating user", errors);
            }
            await UpdateUserRolesAsync(user);

            await HandleLockAndNotificationsAsync(user, portal_base_url, false);

            await tx.CommitAsync();

            return (true, $"User {user.Email} updated successfully", errors);
        }
        private async Task SaveProfileImageAsync(ApplicationUser model, string suffix)
        {
            var safeFolder = Regex.Replace(suffix, @"[^a-zA-Z0-9\-\.]", "");

            var (fileName, path) = await _fileStorage.SaveAsync(model.ProfileImage, safeFolder, "user");

            model.ProfilePictureUrl = path;
            model.ProfilePictureExtension = Path.GetExtension(fileName);
        }
        private static void PopulateUserEntity(ApplicationUser model, string email, string createdBy)
        {
            model.Email = email;
            model.UserName = email;
            model.EmailConfirmed = true;
            model.Password = null;
            model.Updated = DateTime.UtcNow;
            model.UpdatedBy = createdBy;
            model.FirstName = WebUtility.HtmlEncode(model.FirstName);
            model.LastName = WebUtility.HtmlEncode(model.LastName);
            model.PhoneNumber = WebUtility.HtmlEncode(model.PhoneNumber?.TrimStart('0'));
            model.Comments = WebUtility.HtmlEncode(model.Comments);
            model.Addressline = WebUtility.HtmlEncode(model.Addressline);
            model.PinCodeId = model.SelectedPincodeId;
            model.DistrictId = model.SelectedDistrictId;
            model.StateId = model.SelectedStateId;
            model.CountryId = model.SelectedCountryId;
            model.IsVendorAdmin = model.Role == AppRoles.AGENCY_ADMIN;
        }
        private async Task HandleLockAndNotificationsAsync(ApplicationUser user, string portal_base_url, bool created = true)
        {
            await _userManager.SetLockoutEnabledAsync(user, !user.Active);

            if (!user.Active)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                if (created)
                {
                    _notify.Custom($"User {user.Email} created (locked).", 3, "green", "fas fa-user-lock");
                }
                else
                {
                    _notify.Custom($"User {user.Email} edited (locked).", 3, "orange", "fas fa-user-lock");
                }
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                var pincode = await _context.PinCode.Include(p => p.District).Include(p => p.State).Include(p => p.Country).FirstOrDefaultAsync(p => p.PinCodeId == user.PinCodeId);
                var onboardAgent = user.Role == AppConstant.AppRoles.AGENT && string.IsNullOrWhiteSpace(user.MobileUId);
                if (onboardAgent)
                {
                    var vendor = await _context.Vendor.FirstOrDefaultAsync(v => v.VendorId == user.VendorId);
                    string tinyUrl = await urlService.ShortenUrlAsync(vendor.MobileAppUrl);

                    var message = $"Dear {user.FirstName},\n" +
                    $"Click on link below to install the mobile app\n\n" +
                    $"{tinyUrl}\n\n" +
                    $"Thanks\n\n" +
                    $"{portal_base_url}";
                    await _sms.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, message, true);
                    _notify.Custom($"Agent {user.Email} onboarding initiated.", 3, "green", "fas fa-user-check");
                }
                else
                {
                    try
                    {
                        await _sms.DoSendSmsAsync(pincode.Country.Code, pincode.Country.ISDCode + user.PhoneNumber, "User created. \nEmail : " + user.Email + "\n" + portal_base_url);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("SMS sending failed.");
                    }
                    if (created)
                    {
                        _notify.Custom($"User <b> {user.Email}</b> created successfully.", 3, "green", "fas fa-user-check");
                    }
                    else
                    {
                        _notify.Custom($"User {user.Email} edited successfully.", 3, "orange", "fas fa-user-lock");
                    }
                }
            }
        }
        private async Task UpdateGeoLocationAsync(ApplicationUser user)
        {
            if (user.Role != AppRoles.AGENT)
                return;

            var pincode = await _context.PinCode
                .Include(p => p.District)
                .Include(p => p.State)
                .Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.PinCodeId == user.PinCodeId);

            if (pincode == null) return;
            if (user.Role == AppRoles.AGENT)
            {
                var address =
                $"{user.Addressline}, {pincode.Name}, {pincode.District.Name}, {pincode.State.Name}, {pincode.Country.Name}";

                var coordinates = await _geoClient.GetCoordinatesFromAddressAsync(address);

                var latLong = $"{coordinates.Latitude},{coordinates.Longitude}";

                user.AddressLatitude = coordinates.Latitude;
                user.AddressLongitude = coordinates.Longitude;
                user.AddressMapLocation =
                    $"https://maps.googleapis.com/maps/api/staticmap?center={latLong}&zoom=14&size=200x200" +
                    $"&maptype=roadmap&markers=color:red%7C{latLong}" +
                    $"&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
            }

        }
        private async Task UpdateUserRolesAsync(ApplicationUser user)
        {
            var existingRoles = await _userManager.GetRolesAsync(user);

            if (existingRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, existingRoles);

            await _userManager.AddToRoleAsync(user, user.Role.ToString());
        }
        private static void UpdateUserFields(ApplicationUser input, ApplicationUser user, string updatedBy)
        {
            user.ProfilePictureUrl = input.ProfilePictureUrl ?? user.ProfilePictureUrl;
            user.ProfilePictureExtension = input.ProfilePictureExtension ?? user.ProfilePictureExtension;
            user.FirstName = WebUtility.HtmlEncode(input.FirstName);
            user.LastName = WebUtility.HtmlEncode(input.LastName);
            user.Addressline = WebUtility.HtmlEncode(input.Addressline);
            user.Comments = WebUtility.HtmlEncode(input.Comments);
            user.PhoneNumber = input.PhoneNumber?.TrimStart('0');
            user.Active = input.Active;

            user.PinCodeId = input.SelectedPincodeId;
            user.DistrictId = input.SelectedDistrictId;
            user.StateId = input.SelectedStateId;
            user.CountryId = input.SelectedCountryId;

            if (!string.IsNullOrWhiteSpace(input.Password))
                user.Password = input.Password;

            user.Role = input.Role;
            user.IsVendorAdmin = user.Role == AppRoles.AGENCY_ADMIN;
            user.IsUpdated = true;
            user.Updated = DateTime.UtcNow;
            user.UpdatedBy = updatedBy;
            user.SecurityStamp = Guid.NewGuid().ToString();
        }
    }
}
