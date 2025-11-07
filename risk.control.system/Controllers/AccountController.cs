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
using Microsoft.FeatureManagement;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

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
        public async Task<IActionResult> KeepAlive()
        {
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
                currentPassword = user.Password,
                profilePicture = Convert.ToBase64String(user.ProfilePicture) // Ensure it's Base64
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
                model.LoginError = "Invalid credentials.";
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                return View(model);
            }
            var email = HttpUtility.HtmlEncode(model.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || user.Email is null || string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                model.LoginError = "Invalid credentials.";
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                return View(model);
            }

            var admin = _context.ApplicationUser.Include(a => a.Country).FirstOrDefault(u => u.IsSuperAdmin);
            if (admin is null || admin.Country is null)
            {
                ModelState.AddModelError(string.Empty, "Bad Request.");
                model.LoginError = "Server Error. Try again.";
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
                return View(model);
            }
            var pwd = HttpUtility.HtmlEncode(model.Password);
            var result = await _signInManager.PasswordSignInAsync(email, pwd, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
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
                        notifyService.Success($"Welcome <b>{loggingUsername}</b>, Login successful");
                        if (Url.IsLocalUrl(model.ReturnUrl))
                            return Redirect(model.ReturnUrl);

                        return RedirectToAction("Index", "Dashboard");
                    }
                }
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string failedMessage = $"Dear {admin.Email} ,\n" +
                             $"User {user.Email} can't log in. \n" +
                             $"{BaseUrl}";
                    await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + admin.PhoneNumber, failedMessage);
                }
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                _logger.LogCritical("User can't login.");
                model.LoginError = "User can't login.";
                return View(model);
            }
            else if (result.IsLockedOut)
            {
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {admin.Email}, \n" +
                        $"{model.Email} locked out.\n " +
                        $"{BaseUrl}";
                    await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + admin.PhoneNumber, message);
                }
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                _logger.LogError("User account locked out.");
                model.LoginError = "User account locked out.";
                return View(model);
            }
            else if (result.IsNotAllowed)
            {
                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN))
                {
                    string message = $"Dear {admin.Email}, \n" +
                        $"{model.Email} failed login attempt. {nameof(result.IsNotAllowed)}. \n" +
                        $"{BaseUrl}";
                    await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + admin.PhoneNumber, message);
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                model.LoginError = $"{nameof(result.IsNotAllowed)}. Contact admin.";
                model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
                ViewData["Users"] = new SelectList(_context.Users.OrderBy(o => o.Email), "Email", "Email");
                _logger.LogError("User account not allowed.");
                return View(model);
            }
            _logger.LogError("Invalid credentials. Try again.");
            ModelState.AddModelError(string.Empty, "Bad Request.");
            model.LoginError = "Invalid credentials. Try again";
            model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
            ViewData["Users"] = new SelectList(_context.Users.Where(u => !u.Deleted).OrderBy(o => o.Email), "Email", "Email");
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
                Email = email,
                //CurrentPassword = user.Password,
                //ProfilePicture = user.ProfilePicture
            };
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                    return RedirectToAction("Login");
                }

                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {

                    notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

                    return RedirectToAction("Login");
                }

                // Mark that the user has changed their password
                user.Password = model.NewPassword;
                user.Updated = DateTime.Now;
                user.IsPasswordChangeRequired = false;
                await _userManager.UpdateAsync(user);

                await _signInManager.RefreshSignInAsync(user);
                var admin = _context.ApplicationUser.Include(a => a.Country).FirstOrDefault(u => u.IsSuperAdmin);
                var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
                var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
                var BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
                var roles = await _userManager.GetRolesAsync(user);
                if (roles != null && roles.Count > 0)
                {
                    var companyUser = _context.ClientCompanyApplicationUser.FirstOrDefault(u => u.Email == user.Email && !u.Deleted);
                    var vendorUser = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == user.Email && !u.Deleted);
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
                                message = $"Dear {admin.Email}, \n" +
                                $"User {user.Email} logged in. \n" +
                                $"{BaseUrl}";
                                try
                                {
                                    await smsService.DoSendSmsAsync(admin.Country.Code, "+" + admin.Country.ISDCode + admin.PhoneNumber, message);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex.StackTrace);
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                        }

                        notifyService.Custom($"Password update successful", 3, "orange", "fa fa-unlock");
                        return RedirectToAction("Index", "Dashboard");
                    }
                }

                if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) && !user.Email.StartsWith("admin"))
                {
                    var adminForFailed = _context.ApplicationUser.Include(a => a.Country).FirstOrDefault(u => u.IsSuperAdmin);
                    string failedMessage = $"Dear {admin.Email}, \n" +
                        $"User {user.Email} password updated.  \n" +
                        $"{BaseUrl}";
                    await smsService.DoSendSmsAsync(adminForFailed.Country.Code, "+" + adminForFailed.Country.ISDCode + adminForFailed.PhoneNumber, failedMessage);
                }
                notifyService.Custom($"Password update successful", 3, "orange", "fa fa-unlock");
                return RedirectToAction("Index", "Dashboard");
            }
            notifyService.Custom($"Password update Error", 3, "red", "fa fa-lock");

            return View(model);
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(LoginViewModel input)
        {
            string message = "Incorrect details. Try Again";
            string imagePath = Path.Combine(webHostEnvironment.WebRootPath, "img", "no-user.png");
            byte[] image = System.IO.File.ReadAllBytes(imagePath);
            var flagPath = $"/img/no-map.jpeg";
            var model = new Models.ViewModel.ForgotPassword
            {
                Message = message,
                Reset = false,
                Flag = flagPath,
                ProfilePicture = image,
                Email = input.Email
            };
            var user = await _userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return View(model);
            }
            //var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            //// Encode the token to make it URL-safe
            //var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            //// Generate the reset link
            //var resetLink = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, token = encodedToken }, Request.Scheme);

            var smsSent2User = await accountService.ForgotPassword(input.Email, input.Mobile, input.CountryId);
            if (smsSent2User != null)
            {
                model.Message = $"{input.CountryId} (0) {input.Mobile}\n";
                model.Flag = $"/flags/{smsSent2User.Country.Code}.png";
                model.ProfilePicture = smsSent2User.ProfilePicture;
                model.Reset = true;
            }

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
            await _context.SaveChangesAsync(null, false);
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