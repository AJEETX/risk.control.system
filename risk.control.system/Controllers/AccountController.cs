using System;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

using NToastNotify;

using NuGet.Packaging.Signing;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    public class AccountController : Controller
    {
        private readonly IWebHostEnvironment env;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly INotificationService service;
        private readonly IToastNotification toastNotification;
        private readonly IAccountService accountService;
        private readonly ILogger _logger;
        private readonly IFeatureManager featureManager;
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;

        public AccountController(
            IWebHostEnvironment env,
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            INotificationService service,
            IToastNotification toastNotification,
            IAccountService accountService,
            ILogger<AccountController> logger,
            IFeatureManager featureManager,
            INotyfService notifyService,
            ApplicationDbContext context)
        {
            this.env = env;
            _userManager = userManager ?? throw new ArgumentNullException();
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.httpContextAccessor = httpContextAccessor;
            this.service = service;
            this.toastNotification = toastNotification ?? throw new ArgumentNullException();
            this.accountService = accountService;
            this._context = context;
            _logger = logger;
            this.featureManager = featureManager;
            this.notifyService = notifyService;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            var timer = DateTime.Now;
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _signInManager.SignOutAsync();
            var showLoginUsers = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
            if (showLoginUsers)
            {
                ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
            }
            return View(new LoginViewModel { ShowUserOnLogin = showLoginUsers });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(CancellationToken ct, LoginViewModel model)
        {
            var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var ipAddressWithoutPort = ipAddress?.Split(':')[0];

            if (ModelState.IsValid || !model.Email.ValidateEmail())
            {
                var email = HttpUtility.HtmlEncode(model.Email);
                var pwd = HttpUtility.HtmlEncode(model.Password);
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
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

                                });
                            var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;

                            var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, "login-success", model.Email, isAuthenticated);
                            if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) && !user.Email.StartsWith("admin"))
                            {
                                var admin = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin);
                                string message = $"Dear {admin.Email}";
                                message += $"                                       ";
                                message += $"                       ";
                                message += $"User {user.Email} logged in from IP address {ipApiResponse.query}";
                                message += $"                                       ";
                                message += $"Thanks                                         ";
                                message += $"                                       ";
                                message += $"                                       ";
                                message += $"{BaseUrl}";
                                try
                                {
                                    SMS.API.SendSingleMessage("+" + admin.PhoneNumber, message);
                                }
                                catch (Exception)
                                {
                                }
                            }

                            notifyService.Success("Login successful");
                            return RedirectToAction("Index", "Dashboard");
                        }
                    }

                    var ipApiFailedResponse = await service.GetClientIp(ipAddressWithoutPort, ct, "login-failed", model.Email, false);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) && !user.Email.StartsWith("admin"))
                    {
                        var adminForFailed = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin);
                        string failedMessage = $"Dear {adminForFailed.Email}";
                        failedMessage += $"                         ";
                        failedMessage += $"Locked user {user.Email} logged in from IP address {ipApiFailedResponse.query}";
                        failedMessage += $"                                       ";
                        failedMessage += $"Thanks                                       ";
                        failedMessage += $"                                       ";
                        failedMessage += $"                                       ";
                        failedMessage += $"{BaseUrl}";
                        SMS.API.SendSingleMessage("+" + adminForFailed.PhoneNumber, failedMessage);
                    }

                    _logger.LogWarning("User account locked out.");
                    model.Error = "User account locked out.";
                    return RedirectToAction("login", model);
                }
                else if (result.IsLockedOut)
                {
                    var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                    var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, "login-locked", model.Email, isAuthenticated);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        var admin = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin);
                        string message = $"Dear {admin.Email}";
                        message += $"                           ";
                        message += $"{model.Email} failed login attempt from IP address {ipApiResponse.query}";
                        message += $"                                       ";
                        message += $"Thanks                                         ";
                        message += $"                                       ";
                        message += $"                                       ";
                        message += $"{BaseUrl}";
                        SMS.API.SendSingleMessage("+" + admin.PhoneNumber, message);
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
                    var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;

                    var ipApiResponse = await service.GetClientIp(ipAddressWithoutPort, ct, "login-failed", model.Email, isAuthenticated);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                    {
                        var admin = _context.ApplicationUser.FirstOrDefault(u => u.IsSuperAdmin);
                        string message = $"Dear {admin.Email}";
                        message += $"                          ";
                        message += $"{model.Email} failed login attempt from IP address {ipApiResponse.query}";
                        message += $"                                       ";
                        message += $"Thanks                                         ";
                        message += $"                                       ";
                        message += $"                                       ";
                        message += $"{BaseUrl}";
                        SMS.API.SendSingleMessage("+" + admin.PhoneNumber, message);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        model.Error = "Invalid login attempt.";
                        model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                        ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                        return View(model);
                    }
                }
            }
            ModelState.AddModelError(string.Empty, "Bad Request.");
            model.Error = "Bad Request.";
            model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
            ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(LoginViewModel input)
        {
            string message = string.Empty;
            var smsSent = accountService.ForgotPassword(input.Email,long.Parse(input.Mobile));
            if (smsSent)
            {
                message = "Password sent to mobile: " + input.Mobile;
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

    }
}