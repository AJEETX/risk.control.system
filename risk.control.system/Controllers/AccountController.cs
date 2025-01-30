using System;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

using NToastNotify;

using NuGet.Packaging.Signing;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IConfiguration config;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly INotificationService service;
        private readonly IAccountService accountService;
        private readonly ILogger _logger;
        private readonly IFeatureManager featureManager;
        private readonly INotyfService notifyService;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
            IConfiguration config,
             IHttpContextAccessor httpContextAccessor,
            INotificationService service,
            IAccountService accountService,
            ILogger<AccountController> logger,
            IFeatureManager featureManager,
            INotyfService notifyService,
            ISmsService SmsService,
            ApplicationDbContext context)
        {
            _userManager = userManager ?? throw new ArgumentNullException();
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.config = config;
            this.httpContextAccessor = httpContextAccessor;
            this.service = service;
            this.accountService = accountService;
            this._context = context;
            _logger = logger;
            this.featureManager = featureManager;
            this.notifyService = notifyService;
            smsService = SmsService;
        }

        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> KeepSessionAlive([FromBody] KeepSessionRequest request)
        {
            try
            {
                if (User is null || User.Identity is null)
                {
                    return Unauthorized(new { message = "User is logged out due to inactivity or authentication failure." });
                }

                if (User.Identity.IsAuthenticated)
                {
                    var user = await _signInManager.UserManager.GetUserAsync(User);

                    if (user != null)
                    {
                        await _signInManager.RefreshSignInAsync(user);
                        var userDetails = new
                        {
                            name = user.UserName,
                            role = user.Role != null ? user.Role.GetEnumDisplayName() : null!,
                            cookieExpiry = user.LastActivityDate ?? user.Updated,
                            currentPage = request.CurrentPage
                        };

                        var userSessionAlive = new UserSessionAlive
                        {
                            Updated = DateTime.Now,
                            ActiveUser = user,
                            CurrentPage = request.CurrentPage,
                        };
                        _context.UserSessionAlive.Add(userSessionAlive);
                        await _context.SaveChangesAsync();
                        return Ok(userDetails);
                    }
                }
                await _signInManager.SignOutAsync();
                return Unauthorized(new { message = "User is logged out due to inactivity or authentication failure." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KeepSessionAlive");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

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
                var showgtrialUsers = await featureManager.IsEnabledAsync(FeatureFlags.TrialVersion);
                if (showgtrialUsers)
                {
                    var xx = _context.Users.ToList();
                    ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted && !u.Email.StartsWith("admin")).OrderBy(o => o.Email), "Email", "Email");
                }
                else
                {
                    ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                }
            }

            return View(new LoginViewModel { ShowUserOnLogin = showLoginUsers });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            //var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            //var ipAddressWithoutPort = ipAddress?.Split(':')[0];
            if (!ModelState.IsValid || !model.Email.ValidateEmail())
            {
                ModelState.AddModelError(string.Empty, "Bad Request.");
                model.Error = "Bad Request.";
                model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                return View(model);
            }
            var email = HttpUtility.HtmlEncode(model.Email);
            var pwd = HttpUtility.HtmlEncode(model.Password);
            var result = await _signInManager.PasswordSignInAsync(email, pwd, model.RememberMe, lockoutOnFailure: false);
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
            var admin = _context.ApplicationUser.Include(a => a.Country).FirstOrDefault(u => u.IsSuperAdmin);
            if (admin is null || admin.Country is null)
            {
                ModelState.AddModelError(string.Empty, "Bad Request.");
                model.Error = "Bad Request.";
                model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                return View(model);
            }
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user is null || user.Email is null || string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    model.Error = "Invalid login attempt.";
                    model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                    ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                    return View(model);
                }

                if (await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION))
                {
                    if (user.IsPasswordChangeRequired)
                    {
                        return RedirectToAction("ChangePassword", "Account", new { email = user.Email });
                    }
                }
                var roles = await _userManager.GetRolesAsync(user);
                if (roles != null && roles.Count > 0)
                {
                    var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == email && !u.Deleted);
                    var vendorUser = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == email && !u.Deleted);
                    bool vendorIsActive = false;
                    bool companyIsActive = false;

                    if (companyUser != null)
                    {
                        companyIsActive = _context.ClientCompany.Any(c => c.ClientCompanyId == companyUser.ClientCompanyId && c.Status == Models.CompanyStatus.ACTIVE);

                    }
                    else if (vendorUser != null)
                    {
                        vendorIsActive = _context.Vendor.Any(c => c.VendorId == vendorUser.VendorId && c.Status == Models.VendorStatus.ACTIVE);
                        if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED) && vendorIsActive)
                        {
                            var userIsAgent = vendorUser.Role == AppRoles.AGENT;
                            if (userIsAgent)
                            {
                                vendorIsActive = !string.IsNullOrWhiteSpace(user.MobileUId);
                            }
                        }
                    }
                    if (companyIsActive && user.Active || vendorIsActive && user.Active || companyUser == null && vendorUser == null)
                    {
                        var timeout = config["SESSION_TIMEOUT_SEC"];
                        var properties = new AuthenticationProperties
                        {
                            IsPersistent = true, // Makes the cookie persistent
                            ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(double.Parse(timeout ?? "900")), // Reset expiry time
                        };
                        await _signInManager.SignInAsync(user, properties);

                        if (User is null || User.Identity is null)
                        {
                            return Unauthorized(new { message = "User is logged out due to inactivity or authentication failure." });
                        }

                        var isAuthenticated = User.Identity.IsAuthenticated;

                        if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) && user?.Email != null && !user.Email.StartsWith("admin"))
                        {
                            string message = string.Empty;
                            if (admin != null)
                            {
                                message = $"Dear {admin.Email}";
                                message += $"                                       ";
                                message += $"                       ";
                                message += $"User {user.Email} logged in";
                                message += $"                                       ";
                                message += $"Thanks                                         ";
                                message += $"                                       ";
                                message += $"                                       ";
                                message += $"{BaseUrl}";
                                try
                                {
                                    await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + admin.PhoneNumber, message);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                        }

                        notifyService.Success("Login successful");
                        return RedirectToAction("Index", "Dashboard");
                    }
                }

                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) && !user.Email.StartsWith("admin"))
                {
                    var adminForFailed = _context.ApplicationUser.Include(a => a.Country).FirstOrDefault(u => u.IsSuperAdmin);
                    string failedMessage = $"Dear {admin.Email}";
                    failedMessage += $"                         ";
                    failedMessage += $"Locked user {user.Email} logged in";
                    failedMessage += $"                                       ";
                    failedMessage += $"Thanks                                       ";
                    failedMessage += $"                                       ";
                    failedMessage += $"                                       ";
                    failedMessage += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync("+" + adminForFailed.Country.ISDCode + adminForFailed.PhoneNumber, failedMessage);
                }
                model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                _logger.LogWarning("User account locked out.");
                model.Error = "User account locked out.";
                return View(model);
            }
            else if (result.IsLockedOut)
            {
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {admin.Email}";
                    message += $"                           ";
                    message += $"{model.Email} failed login attempt";
                    message += $"                                       ";
                    message += $"Thanks                                         ";
                    message += $"                                       ";
                    message += $"                                       ";
                    message += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + admin.PhoneNumber, message);
                }
                else
                {
                    model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                    ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                    _logger.LogWarning("User account locked out.");
                    model.Error = "User account locked out.";
                    return View(model);
                }
            }
            else
            {
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {admin.Email}";
                    message += $"                          ";
                    message += $"{model.Email} failed login attempt";
                    message += $"                                       ";
                    message += $"Thanks                                         ";
                    message += $"                                       ";
                    message += $"                                       ";
                    message += $"{BaseUrl}";
                    await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + admin.PhoneNumber, message);
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
            ModelState.AddModelError(string.Empty, "Bad Request.");
            model.Error = "Bad Request.";
            model.ShowUserOnLogin = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
            ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword(string email)
        {
            var model = new ChangePasswordViewModel
            {
                Email = email
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                }

                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {

                    notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                    return View(model);
                }

                // Mark that the user has changed their password
                user.IsPasswordChangeRequired = false;
                await _userManager.UpdateAsync(user);

                await _signInManager.RefreshSignInAsync(user);

                notifyService.Custom($"Password update successful", 3, "orange", "fa fa-unlock");
                return RedirectToAction("Index", "Dashboard");
            }

            return View(model);
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(LoginViewModel input)
        {
            string message = string.Empty;
            var user = await _userManager.FindByEmailAsync(input.Email);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encode the token to make it URL-safe
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Generate the reset link
            var resetLink = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, token = encodedToken }, Request.Scheme);

            var smsSent = await accountService.ForgotPassword(input.Email, long.Parse(input.Mobile));
            if (smsSent)
            {
                message = "Password sent to mobile: " + input.Mobile;
            }
            else
            {
                message = "Incorrect details. Try Again";
            }
            var model = new Models.ViewModel.ForgotPassword
            {
                Message = message,
                Reset = smsSent
            };
            return View(model);
        }
        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (userId == null || token == null)
                return BadRequest("Invalid password reset token.");

            var model = new ResetPasswordViewModel { UserId = userId, Token = token };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var user = await _signInManager.UserManager.GetUserAsync(User);
            var userSessionAlive = new UserSessionAlive
            {
                Updated = DateTime.Now,
                ActiveUser = user,
                CurrentPage = "Logging-Out",
                LoggedOut = true
            };
            _context.UserSessionAlive.Add(userSessionAlive);
            await _context.SaveChangesAsync();
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

            var userCount = await _userManager.Users.CountAsync(u => u.Email.Trim().ToLower().Substring(u.Email.IndexOf("@") + 1) == newDomain);

            return userCount == 0 ? 0 : 1;

        }

        [HttpGet]
        public async Task<int?> CheckAgencyName(string input, string domain)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(domain))
            {
                return null;
            }
            Domain domainData = (Domain)Enum.Parse(typeof(Domain), domain, true);

            var newDomain = input.Trim().ToLower() + domainData.GetEnumDisplayName();

            var agenccompanyCount = await _context.ClientCompany.CountAsync(u => u.Email.Trim().ToLower() == newDomain);
            var agencyCount = await _context.Vendor.CountAsync(u => u.Email.Trim().ToLower() == newDomain);

            return agencyCount == 0 && agenccompanyCount == 0 ? 0 : 1;
        }

        [HttpGet]
        public async Task<int?> CheckUserEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var userCount = await _userManager.Users.CountAsync(u => u.Email == input);

            return userCount == 0 ? 0 : 1;
        }

    }
    public class KeepSessionRequest
    {
        public string CurrentPage { get; set; }
    }
}