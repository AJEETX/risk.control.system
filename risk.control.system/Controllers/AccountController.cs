﻿using System.Security.Claims;
using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using NToastNotify;

using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IToastNotification toastNotification;
        private readonly IAccountService accountService;
        private readonly ILogger _logger;
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
            IToastNotification toastNotification,
            IAccountService accountService,
            ILogger<AccountController> logger,
            INotyfService notifyService,
            ApplicationDbContext context)
        {
            _userManager = userManager ?? throw new ArgumentNullException();
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.toastNotification = toastNotification ?? throw new ArgumentNullException();
            this.accountService = accountService;
            this._context = context;
            _logger = logger;
            this.notifyService = notifyService;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid || !model.Email.ValidateEmail())
            {
                ViewData["ReturnUrl"] = returnUrl == "dashboard";
                var email = HttpUtility.HtmlEncode(model.Email);
                var pwd = HttpUtility.HtmlEncode(model.Password);
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user.Active)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles != null && roles.Count > 0)
                        {
                            var claims = new List<Claim> {
                            new Claim(ClaimTypes.NameIdentifier, model.Email) ,
                            new Claim(ClaimTypes.Name, model.Email)
                            };
                            var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);

                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                                new AuthenticationProperties
                                {
                                    IsPersistent = false,
                                    AllowRefresh = true,
                                    ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
                                });
                            if (model.Mobile)
                            {
                                return Ok();
                            }
                            notifyService.Success("Login successful");
                            return RedirectToLocal(returnUrl);
                        }
                    }
                    _logger.LogWarning("User account locked out.");
                    model.Error = "User account locked out.";
                    return RedirectToAction("login", model);
                }
                else if (result.IsLockedOut)
                {
                    if (model.Mobile)
                    {
                        return BadRequest();
                    }
                    else
                    {
                        _logger.LogWarning("User account locked out.");
                        model.Error = "User account locked out.";
                        return View(model);
                    }
                }
                else
                {
                    if (model.Mobile)
                    {
                        return BadRequest();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        model.Error = "Invalid login attempt.";
                        return View(model);
                    }
                }
            }
            if (model.Mobile)
            {
                return BadRequest();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Bad Request.");
                model.Error = "Bad Request.";
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Forgot(string useremail, long mobile)
        {
            string message = string.Empty;
            var smsSent = accountService.ForgotPassword(useremail, mobile);
            if (smsSent)
            {
                message = "Password sent to mobile: " + mobile;
                notifyService.Success(message);
            }
            else
            {
                message = "Incorrect details. Try Again";
                notifyService.Error(message);
            }
            ForgotPassword model = new ForgotPassword
            {
                Message = message,
                Reset = smsSent
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(AccountController.Login), "Account");
        }

        [HttpGet]
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