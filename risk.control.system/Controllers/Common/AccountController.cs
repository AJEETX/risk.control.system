using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Common
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFeatureManager _featureManager;
        private readonly ILoginService _loginService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly INotyfService _notifyService;
        private readonly string _baseUrl;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IFeatureManager featureManager,
            ILoginService loginService,
            SignInManager<ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            IAccountService accountService,
            ILogger<AccountController> logger,
            INotyfService notifyService)
        {
            _userManager = userManager ?? throw new ArgumentNullException();
            this._featureManager = featureManager;
            this._loginService = loginService;
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this._accountService = accountService;
            _logger = logger;
            this._notifyService = notifyService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            var setPassword = await _featureManager.IsEnabledAsync(FeatureFlags.SHOW_PASSWORD);
            if (!string.IsNullOrEmpty(returnUrl) && !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            return View(new LoginViewModel
            {
                SetPassword = setPassword,
                ReturnUrl = returnUrl ?? "/"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string agent_login = "")
        {
            if (!ModelState.IsValid || !model.Email.ValidateEmail())
                return await PrepareInvalidView(model, "Bad Request.");
            try
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (!result.Succeeded)
                    return await PrepareInvalidView(model, _loginService.GetErrorMessage(result));

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return await PrepareInvalidView(model, "User can't login.");
                var (isAuthorized, displayName, isAdmin) = await _loginService.GetUserStatusAsync(user, agent_login);

                if (!isAuthorized)
                    return await PrepareInvalidView(model, "Account inactive or unauthorized.");

                bool forceChangeEnabled = await _featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);

                if (!isAdmin && forceChangeEnabled && user.IsPasswordChangeRequired)
                {
                    return RedirectToAction("ChangePassword", "Account", new { email = user.Email });
                }

                await _loginService.SignInWithTimeoutAsync(user);

                _notifyService.Success($"Welcome <b>{displayName}</b>, Login successful");
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Login for {UserEmail}", User.Identity.Name ?? "Anonymous");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AzureLogin(string returnUrl = "/")
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var email = User?.Identity?.Name;

            await _accountService.Logout(email);

            _logger.LogInformation("User logged out.");

            return RedirectToAction(nameof(Login), ControllerName<AccountController>.Name, new { returnUrl = "/" });
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                return RedirectToAction(nameof(Login));
            }
            var model = new ChangePasswordViewModel
            {
                Email = user.Email,
                Id = user.Id
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                return View(model);
            }
            try
            {
                var result = await _accountService.ChangePasswordAsync(model, User, HttpContext.User.Identity.IsAuthenticated, _baseUrl);

                if (!result.Success)
                {
                    _notifyService.Error(result.Message);

                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    return View(model);
                }

                _notifyService.Custom($"Password update successful", 3, "orange", "fa fa-unlock");
                return this.RedirectToAction<DashboardController>(x => x.Index());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password for {UserEmail}", User.Identity.Name ?? "Anonymous");
                _notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(ForgotPasswordViewModel input)
        {
            ForgotPassword model;
            if (!ModelState.IsValid)
            {
                model = await _accountService.CreateDefaultForgotPasswordModel(input?.Email);
                return View(model);
            }
            try
            {
                var smsResult = await _accountService.ForgotPassword(input.Email, input.Mobile, input.CountryId);

                if (smsResult == null)
                {
                    model = await _accountService.CreateDefaultForgotPasswordModel(input?.Email);
                    return View(model);
                }
                var successModel = new ForgotPassword
                {
                    Id = smsResult.Id,
                    Email = input.Email,
                    Message = $"{input.CountryId} (0) {input.Mobile}",
                    Flag = $"/flags/{smsResult.CountryCode}.png",
                    ProfilePicture = smsResult.ProfilePicture,
                    Reset = true,
                    ProfileImage = smsResult.ProfileImage
                };

                return View(successModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forgot Password for {UserEmail}", input?.Email ?? "Anonymous"); ;
                throw;
            }
        }

        private async Task<IActionResult> PrepareInvalidView(LoginViewModel model, string error)
        {
            model.LoginError = error;
            ModelState.AddModelError(string.Empty, error);
            model.SetPassword = await _featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
            return View(model);
        }
    }
}