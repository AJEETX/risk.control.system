﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using NToastNotify;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class AccountController : Controller
    {
        private const string mobileAppUrl = "http://localhost:19006/";
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IToastNotification toastNotification;
        private readonly ILogger _logger;

        public AccountController(
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
            IToastNotification toastNotification,
            ILogger<AccountController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException();
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.toastNotification = toastNotification ?? throw new ArgumentNullException();
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
            ViewData["ReturnUrl"] = returnUrl;
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded && returnUrl != mobileAppUrl)
            {
                _logger.LogInformation("User logged in.");
                toastNotification.AddSuccessToastMessage("login successful!");
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut && returnUrl != mobileAppUrl)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToAction(nameof(Lockout));
            }
            else if (result.Succeeded && returnUrl == mobileAppUrl)
            {
                return Ok(new { success = "SUCCESS" });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                model.Error = "Invalid login attempt.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(ContactMessageController.Index), "ContactMessage");
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
                return RedirectToAction(nameof(ContactMessageController.Index), "ContactMessage");
            }
        }

        private IActionResult RedirectToConfirmEmailNotification()
        {
            return Redirect("/Account/ConfirmEmailNotification");
        }
    }
}