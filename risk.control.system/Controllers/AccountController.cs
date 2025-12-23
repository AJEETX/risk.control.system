using System.Web;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration config;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly INotificationService service;
        private readonly IAccountService accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly IFeatureManager featureManager;
        private readonly INotyfService notifyService;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext _context;
        private readonly string BaseUrl;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            SignInManager<ApplicationUser> signInManager,
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
            this.webHostEnvironment = webHostEnvironment;
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
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KeepSessionAlive([FromBody] KeepSessionRequest request)
        {
            try
            {
                if (ModelState.IsValid == false)
                {
                    return BadRequest(new { message = "Invalid request." });
                }
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
                        await _context.SaveChangesAsync(null, false);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KeepAlive()
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(new { message = "Invalid request." });
            }
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.LastActivityDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }
            }
            return Ok();
        }
        [HttpGet]
        public async Task StreamTypingUpdates(string email, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";

            // Fetch user details for password change
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                await Response.WriteAsync($"data: ERROR_UserNotFound\n");
                await Response.WriteAsync($"data: done\n");
                await Response.Body.FlushAsync(cancellationToken);
                return;
            }

            // Send user details first
            var passwordModelJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                email = user.Email,
                currentPassword = user.Password
            });

            await Response.WriteAsync($"data: PASSWORD_UPDATE|{passwordModelJson}\n");
            await Response.Body.FlushAsync(cancellationToken);
            await Task.Delay(1000, cancellationToken); // Small delay to ensure UI updates first

            // Now, stream messages one by one
            var messages = new List<string>
            {
                $"Welcome ! {user.Email} First time user.",
                "Please update password to continue.",
                "Remember password for later."
            };

            foreach (var message in messages)
            {
                await Response.WriteAsync($"data: {message}\n");
                await Response.Body.FlushAsync(cancellationToken);
                await Task.Delay(1500, cancellationToken); // Simulate delay between messages
            }

            // Indicate completion
            await Response.WriteAsync($"data: done\n");
            await Response.Body.FlushAsync(cancellationToken);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            var timer = DateTime.Now;
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _signInManager.SignOutAsync();
            var setPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_PASSWORD);
            return View(new LoginViewModel { SetPassword = setPassword, OtpLogin = await featureManager.IsEnabledAsync(FeatureFlags.OTP_LOGIN), ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string agent_login = "")
        {
            if (!ModelState.IsValid || !model.Email.ValidateEmail())
            {
                ModelState.AddModelError(string.Empty, "Bad Request.");
                model.LoginError = "Bad Request.";
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                return View(model);
            }
            var email = HttpUtility.HtmlEncode(model.Email);
            var pwd = HttpUtility.HtmlEncode(model.Password);
            var result = await _signInManager.PasswordSignInAsync(email, pwd, model.RememberMe, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                    ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                    _logger.LogError("User account locked out.");
                    model.LoginError = "User account locked out.";
                    return View(model);
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    model.LoginError = $"{nameof(result.IsNotAllowed)}. Contact admin.";
                    model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                    ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                    _logger.LogError("User account not allowed.");
                    return View(model);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid credentials.");
                    model.LoginError = $"Invalid credentials. Contact admin.";
                    model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                    ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                    _logger.LogError("Invalid credentials.");
                    return View(model);
                }
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                _logger.LogCritical("User can't login.");
                model.LoginError = "User can't login.";
                return View(model);
            }
            var admin = await _userManager.IsInRoleAsync(user, AppRoles.PORTAL_ADMIN.ToString());
            if (!admin)
            {
                if (await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION))
                {
                    if (user.IsPasswordChangeRequired)
                    {
                        return RedirectToAction("ChangePassword", "Account", new { email = user.Email });
                    }
                }
            }
            var companyUser = await _context.ClientCompanyApplicationUser.FirstOrDefaultAsync(u => u.Email == email && !u.Deleted);
            var vendorUser = await _context.VendorApplicationUser.FirstOrDefaultAsync(u => u.Email == email && !u.Deleted);
            bool vendorIsActive = false;
            bool companyIsActive = false;
            string loggingUsername = "Admin";
            if (companyUser != null)
            {
                companyIsActive = _context.ClientCompany.Any(c => c.ClientCompanyId == companyUser.ClientCompanyId && c.Status == Models.CompanyStatus.ACTIVE);
                loggingUsername = companyUser.FirstName;
            }
            else if (vendorUser != null)
            {
                loggingUsername = vendorUser.FirstName;
                vendorIsActive = _context.Vendor.Any(c => c.VendorId == vendorUser.VendorId && c.Status == Models.VendorStatus.ACTIVE);
                if (agent_login != "agent_login")
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED) && vendorIsActive)
                    {
                        var userIsAgent = vendorUser.Role == AppRoles.AGENT;
                        if (userIsAgent)
                        {
                            if (await featureManager.IsEnabledAsync(FeatureFlags.AGENT_LOGIN_DISABLED_ON_PORTAL))
                            {
                                vendorIsActive = false;
                            }
                            else
                            {
                                vendorIsActive = !string.IsNullOrWhiteSpace(user.MobileUId);
                            }
                        }
                    }
                }
            }

            if (
                (companyIsActive && user.Active) ||  // Company user active
                (vendorIsActive && user.Active) ||  // Vendor user active
                (companyUser == null && vendorUser == null) // SuperAdmin user 
                )
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
                notifyService.Success($"Welcome <b>{loggingUsername}</b>, Login successful");
                if (Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Dashboard");
            }

            model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
            ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
            _logger.LogCritical("User can't login.");
            model.LoginError = "User can't login.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                return RedirectToAction("Login");
            }
            var model = new ChangePasswordViewModel
            {
                Email = user.Email
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                return View(model);
            }
            try
            {
                var result = await accountService.ChangePasswordAsync(model, User, HttpContext.User.Identity.IsAuthenticated, BaseUrl);

                if (!result.Success)
                {
                    notifyService.Error(result.Message);

                    foreach (var error in result.Errors)
                        ModelState.AddModelError(error.Key, error.Value);

                    return View(model);
                }

                notifyService.Custom($"Password update successful", 3, "orange", "fa fa-unlock");
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password");
                notifyService.Error("OOPS !!!..Contact Admin");
                return RedirectToAction(nameof(Login));
            }
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(ForgotPasswordViewModel input)
        {
            ForgotPassword model = null;

            if (!ModelState.IsValid)
            {
                model = await CreateDefaultForgotPasswordModel(input?.Email);
                return View(model);
            }
            var smsResult = await accountService.ForgotPassword(input.Email, input.Mobile, input.CountryId);

            if (smsResult == null)
            {
                model = await CreateDefaultForgotPasswordModel(input?.Email);
                return View(model);
            }
            var successModel = new ForgotPassword
            {
                Email = input.Email,
                Message = $"{input.CountryId} (0) {input.Mobile}",
                Flag = $"/flags/{smsResult.CountryCode}.png",
                ProfilePicture = smsResult.ProfilePicture,
                Reset = true
            };

            return View(successModel);
        }
        private async Task<ForgotPassword> CreateDefaultForgotPasswordModel(string email)
        {
            var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-user.png");

            byte[] profilePicture = Array.Empty<byte>();

            if (System.IO.File.Exists(imagePath))
            {
                profilePicture = await System.IO.File.ReadAllBytesAsync(imagePath);
            }

            return new ForgotPassword
            {
                Message = "Incorrect details. Try Again",
                Reset = false,
                Flag = "/img/no-map.jpeg",
                ProfilePicture = profilePicture,
                Email = email
            };
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
            await _context.SaveChangesAsync(null, false);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(AccountController.Login), "Account");
        }
    }
}