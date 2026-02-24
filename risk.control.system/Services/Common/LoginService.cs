using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Common
{
    public interface ILoginService
    {
        Task<(bool IsAuthorized, string DisplayName, bool IsAdmin)> GetUserStatusAsync(ApplicationUser user, string agentLogin = "");

        Task<IEnumerable<SelectListItem>> GetUserSelectListAsync();

        Task SignInWithTimeoutAsync(ApplicationUser user); // Moved here

        string GetErrorMessage(SignInResult result);

        Task<bool> SendOtpAsync(OtpRequest request);

        Task<(bool Success, string Message)> ResendOtpAsync(OtpRequest request);

        Task<(bool Success, string Message)> VerifyAndLoginAsync(OtpLoginModel request);
    }

    internal class LoginService : ILoginService
    {
        private readonly IDbContextFactory<ApplicationDbContext> contextFactory;
        private readonly IMemoryCache cache;
        private readonly ISmsService smsService;
        private readonly IFeatureManager _featureManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration config;
        private readonly SignInManager<ApplicationUser> signInManager;

        public LoginService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IMemoryCache cache,
            ISmsService smsService,
            IFeatureManager featureManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            SignInManager<ApplicationUser> signInManager)
        {
            this.contextFactory = contextFactory;
            this.cache = cache;
            this.smsService = smsService;
            _featureManager = featureManager;
            _userManager = userManager;
            this.config = config;
            this.signInManager = signInManager;
        }

        public async Task<(bool IsAuthorized, string DisplayName, bool IsAdmin)> GetUserStatusAsync(ApplicationUser user, string agentLogin = "")
        {
            // 1. Admin Logic
            if (await _userManager.IsInRoleAsync(user, PORTAL_ADMIN.DISPLAY_NAME))
            {
                return (true, "Admin", true); // IsAdmin = true
            }

            // 2. Company User Logic
            await using var _context = contextFactory.CreateDbContext();
            var companyUser = await _context.ApplicationUser
                .FirstOrDefaultAsync(u => u.Email == user.Email && !u.Deleted && u.ClientCompanyId > 0);

            if (companyUser != null)
            {
                var companyActive = await _context.ClientCompany.AnyAsync(c =>
                    c.ClientCompanyId == companyUser.ClientCompanyId && c.Status == CompanyStatus.ACTIVE);
                return (companyActive && user.Active, companyUser.FirstName, false);
            }

            // 3. Vendor User Logic
            var vendorUser = await _context.ApplicationUser
                .FirstOrDefaultAsync(u => u.Email == user.Email && !u.Deleted && u.VendorId > 0);

            if (vendorUser != null)
            {
                bool vendorActive = await _context.Vendor.AnyAsync(c =>
                    c.VendorId == vendorUser.VendorId && c.Status == VendorStatus.ACTIVE);

                if (agentLogin != "agent_login" && vendorActive)
                {
                    if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED) && vendorUser.Role == AppRoles.AGENT)
                    {
                        if (await _featureManager.IsEnabledAsync(FeatureFlags.AGENT_LOGIN_DISABLED_ON_PORTAL))
                            vendorActive = false;
                        else
                            vendorActive = !string.IsNullOrWhiteSpace(user.MobileUId);
                    }
                }
                return (vendorActive && user.Active, vendorUser.FirstName, false);
            }

            return (false, string.Empty, false);
        }

        public async Task<IEnumerable<SelectListItem>> GetUserSelectListAsync()
        {
            await using var _context = contextFactory.CreateDbContext();
            var users = await _context.Users
                .Where(u => !u.Deleted)
                .OrderBy(o => o.Email)
                .Select(u => new SelectListItem { Text = u.Email, Value = u.Email })
                .ToListAsync();
            return users;
        }

        public async Task SignInWithTimeoutAsync(ApplicationUser user)
        {
            var timeout = config["SESSION_TIMEOUT_SEC"] ?? "900";
            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(double.Parse(timeout))
            };
            await signInManager.SignInAsync(user, properties);
        }

        public string GetErrorMessage(SignInResult result)
        {
            if (result.IsLockedOut) return "User account locked out.";
            if (result.IsNotAllowed) return "User account not allowed. Contact admin.";
            return "Invalid credentials. Contact admin.";
        }

        public async Task<bool> SendOtpAsync(OtpRequest request)
        {
            var result = await InternalSendOtpLogic(request);
            return result.Success;
        }

        public async Task<(bool Success, string Message)> ResendOtpAsync(OtpRequest request)
        {
            return await InternalSendOtpLogic(request);
        }

        public async Task<(bool Success, string Message)> VerifyAndLoginAsync(OtpLoginModel request)
        {
            string cleanIsd = request.CountryIsd.TrimStart('+');
            string cleanMobile = request.MobileNumber.TrimStart('0');
            string cacheKey = $"{cleanIsd}{cleanMobile}";

            if (!cache.TryGetValue(cacheKey, out string correctOtp) || correctOtp != request.UserEnteredOtp?.Trim())
            {
                return (false, "The OTP entered is invalid or has expired.");
            }

            cache.Remove(cacheKey);

            var username = $"{cleanIsd}{cleanMobile}@icheckify.co.in";
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                await using var _context = contextFactory.CreateDbContext();
                var country = await _context.Country.FirstOrDefaultAsync(c => c.ISDCode.ToString() == cleanIsd);

                user = new ApplicationUser
                {
                    FirstName = "Guest",
                    LastName = "Guest",
                    PhoneNumber = cleanMobile,
                    UserName = username,
                    Email = username,
                    CountryId = country?.CountryId ?? 0
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                    return (false, result.Errors.FirstOrDefault()?.Description ?? "User creation failed.");

                await _userManager.AddToRoleAsync(user, GUEST.DISPLAY_NAME);
            }

            // 5. Sign In
            await SignInWithTimeoutAsync(user);

            return (true, "Login successful.");
        }

        private async Task<(bool Success, string Message)> InternalSendOtpLogic(OtpRequest request)
        {
            string cleanIsd = request.CountryIsd?.TrimStart('+') ?? "";
            string cleanMobile = request.MobileNumber?.TrimStart('0') ?? "";
            string cacheKey = $"{cleanIsd}{cleanMobile}";

            // 1. Generate & Cache
            var otp = new Random().Next(1000, 9999).ToString();
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSize(1);
            cache.Set(cacheKey, otp, cacheOptions);

            // 2. Database Lookup
            await using var _context = contextFactory.CreateDbContext();
            var country = await _context.Country.FirstOrDefaultAsync(c => c.ISDCode.ToString() == cleanIsd);
            if (country == null) return (false, "Invalid country code.");

            // 3. Send SMS
            var message = $"Hi user {request.CountryIsd} {cleanMobile}\n" +
                          $"Your code is {otp}\n" +
                          $"{request.BaseUrl}";

            var response = await smsService.SendSmsAsync(country.Code, cleanIsd + cleanMobile, message);

            if (!string.IsNullOrWhiteSpace(response))
                return (true, "OTP sent successfully!");

            return (false, "Failed to send SMS.");
        }
    }
}