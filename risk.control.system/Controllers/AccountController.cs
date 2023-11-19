using System.Net.Mail;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json.Linq;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class AccountController : Controller
    {
        private const string mobileAppUrl = "mobileapp";
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IToastNotification toastNotification;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
            IToastNotification toastNotification,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager ?? throw new ArgumentNullException();
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.toastNotification = toastNotification ?? throw new ArgumentNullException();
            this._context = context;
            _logger = logger;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl == "dashboard";
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!model.Mobile)
                {
                    toastNotification.AddSuccessToastMessage("<i class='fas fa-bookmark'></i> Login successful!");
                    return RedirectToLocal(returnUrl);
                }
                var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, model.Email) ,
                        new Claim(ClaimTypes.Email, model.Email)
                    };
                var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                HttpContext.Response.Cookies.Append("UserLoginCookie", "UserLoginCookie", new CookieOptions() { HttpOnly = true });
                return Ok(new { success = model.Email });
            }

            if (result.IsLockedOut && returnUrl != mobileAppUrl)
            {
                _logger.LogWarning("User account locked out.");
                model.Error = "User account locked out.";
                return View(model);
            }
            else if (result.Succeeded && returnUrl == mobileAppUrl)
            {
                var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, model.Email) ,
                        new Claim(ClaimTypes.Email, model.Email)
                    };
                var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                HttpContext.Response.Cookies.Append("UserLoginCookie", "UserLoginCookie", new CookieOptions() { HttpOnly = true });
                return Ok(new { success = model.Email });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                model.Error = "Invalid login attempt.";
                return View(model);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Forgot(string useremail, long mobile)
        {
            string connectionString = "";
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<int?> CheckUserName(string input, string domain)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(domain))
            {
                return null;
            }
            Domain domainData = (Domain)Enum.Parse(typeof(Domain), domain, true);

            var newDomain = input.Trim().ToLower() + domainData.GetEnumDisplayName();

            var allUsers = _userManager.Users.Where(u =>
            u.Email.Substring(u.Email.IndexOf("@") + 1) == newDomain
            )?.ToList();

            if (allUsers?.Count == 0)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<int?> CheckUserEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var allUsers = _userManager.Users.Where(u =>
            u.Email == input
            )?.ToList();

            if (allUsers?.Count == 0)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
            }
        }

        private IActionResult RedirectToConfirmEmailNotification()
        {
            return Redirect("/Account/ConfirmEmailNotification");
        }
    }
}