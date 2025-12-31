using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ILoginService
    {
        Task<(bool IsAuthorized, string DisplayName, bool IsAdmin)> GetUserStatusAsync(ApplicationUser user, string agentLogin = "");
        Task<IEnumerable<SelectListItem>> GetUserSelectListAsync();
        Task SignInWithTimeoutAsync(ApplicationUser user); // Moved here
        string GetErrorMessage(Microsoft.AspNetCore.Identity.SignInResult result);
    }

    internal class LoginService : ILoginService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureManager _featureManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration config;
        private readonly SignInManager<ApplicationUser> signInManager;

        public LoginService(
            ApplicationDbContext context,
            IFeatureManager featureManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _featureManager = featureManager;
            _userManager = userManager;
            this.config = config;
            this.signInManager = signInManager;
        }

        public async Task<(bool IsAuthorized, string DisplayName, bool IsAdmin)> GetUserStatusAsync(ApplicationUser user, string agentLogin)
        {
            // 1. Admin Logic
            if (await _userManager.IsInRoleAsync(user, AppRoles.PORTAL_ADMIN.ToString()))
            {
                return (true, "Admin", true); // IsAdmin = true
            }
            // 2. Company User Logic
            var companyUser = await _context.ClientCompanyApplicationUser
                .FirstOrDefaultAsync(u => u.Email == user.Email && !u.Deleted);

            if (companyUser != null)
            {
                var companyActive = await _context.ClientCompany.AnyAsync(c =>
                    c.ClientCompanyId == companyUser.ClientCompanyId && c.Status == Models.CompanyStatus.ACTIVE);
                return (companyActive && user.Active, companyUser.FirstName, false);
            }

            // 3. Vendor User Logic
            var vendorUser = await _context.VendorApplicationUser
                .FirstOrDefaultAsync(u => u.Email == user.Email && !u.Deleted);

            if (vendorUser != null)
            {
                bool vendorActive = await _context.Vendor.AnyAsync(c =>
                    c.VendorId == vendorUser.VendorId && c.Status == Models.VendorStatus.ACTIVE);

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

        public string GetErrorMessage(Microsoft.AspNetCore.Identity.SignInResult result)
        {
            if (result.IsLockedOut) return "User account locked out.";
            if (result.IsNotAllowed) return "User account not allowed. Contact admin.";
            return "Invalid credentials. Contact admin.";
        }
    }
}
