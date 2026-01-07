using System.Security.Claims;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        private readonly Dictionary<string, string> azureRoleMapping;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IConfiguration configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoginService loginService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAccountService accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly IFeatureManager featureManager;
        private readonly INotyfService notifyService;
        private readonly ApplicationDbContext _context;
        private readonly string BaseUrl;

        public AccountController(
            IOptions<AzureAdRoleMapping> azureRoleMapping,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILoginService loginService,
            IWebHostEnvironment webHostEnvironment,
            SignInManager<ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            IAccountService accountService,
            ILogger<AccountController> logger,
            IFeatureManager featureManager,
            INotyfService notifyService,
            ApplicationDbContext context)
        {
            this.azureRoleMapping = azureRoleMapping.Value.Roles;
            this.roleManager = roleManager;
            this.configuration = configuration;
            _userManager = userManager ?? throw new ArgumentNullException();
            this.loginService = loginService;
            this.webHostEnvironment = webHostEnvironment;
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.accountService = accountService;
            this._context = context;
            _logger = logger;
            this.featureManager = featureManager;
            this.notifyService = notifyService;
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
        public async Task<IActionResult> Login()
        {
            var setPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_PASSWORD);
            return View(new LoginViewModel { SetPassword = setPassword });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string agent_login = "")
        {
            if (!ModelState.IsValid || !model.Email.ValidateEmail())
                return await PrepareInvalidView(model, "Bad Request.");

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

            if (!result.Succeeded)
                return await PrepareInvalidView(model, loginService.GetErrorMessage(result));

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return await PrepareInvalidView(model, "User can't login.");
            var (isAuthorized, displayName, isAdmin) = await loginService.GetUserStatusAsync(user, agent_login);

            if (!isAuthorized)
                return await PrepareInvalidView(model, "Account inactive or unauthorized.");

            bool forceChangeEnabled = await featureManager.IsEnabledAsync(FeatureFlags.FIRST_LOGIN_CONFIRMATION);

            if (!isAdmin && forceChangeEnabled && user.IsPasswordChangeRequired)
            {
                return RedirectToAction("ChangePassword", "Account", new { email = user.Email });
            }

            await loginService.SignInWithTimeoutAsync(user);

            notifyService.Success($"Welcome <b>{displayName}</b>, Login successful");

            return RedirectToAction("Index", "Dashboard");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AzureLogin(string returnUrl = "/Dashboard/Index")
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/Dashboard/Index")
        {
            var result = await HttpContext.AuthenticateAsync(
                OpenIdConnectDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var principal = result.Principal;

            var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("preferred_username") ?? principal.FindFirstValue(ClaimTypes.Upn);

            if (string.IsNullOrEmpty(email))
                return View("Error", "Email not received from Azure");

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var azureRole = principal.FindFirstValue(ClaimTypes.Role);
                ClientCompany company = null;
                Vendor vendor = null;
                if (RoleGroups.CompanyRoles.Contains(azureRole))
                {
                    company = await _context.ClientCompany.FirstOrDefaultAsync(c => !c.Deleted);
                }
                if (RoleGroups.AgencyRoles.Contains(azureRole))
                {
                    vendor = await _context.Vendor.FirstOrDefaultAsync(c => !c.Deleted);
                }
                string countryCode = string.Empty;
                var countryClaim = principal.Claims.FirstOrDefault(p => p.Type == "ctry");
                if (countryClaim != null)
                {
                    countryCode = countryClaim.Value;
                }
                countryClaim = principal.Claims.First(p => p.Type == "tenant_ctry");
                if (countryClaim != null)
                {
                    countryCode = countryClaim.Value;
                }
                countryCode = principal.Claims.First(p => p.Type == "tenant_ctry").Value;
                var pincode = await _context.PinCode.Include(d => d.District).Include(s => s.State).Include(c => c.Country).FirstOrDefaultAsync(p => p.Country.Code == countryCode);
                var appRole = (AppRoles)Enum.Parse(typeof(AppRoles), azureRole);
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? "",
                    LastName = principal.FindFirstValue(ClaimTypes.Surname) ?? "",
                    PhoneNumber = principal.FindFirstValue(ClaimTypes.MobilePhone) ?? "",
                    Active = true,
                    EmailConfirmed = true,
                    CountryId = pincode.CountryId,
                    StateId = pincode.StateId,
                    DistrictId = pincode.DistrictId,
                    PinCodeId = pincode.PinCodeId,
                    VendorId = vendor?.VendorId,
                    ClientCompanyId = company?.ClientCompanyId,
                    Role = appRole
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    return View("Error",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
                if (!await roleManager.RoleExistsAsync(azureRole))
                {
                    await roleManager.CreateAsync(
                        new ApplicationRole
                        {
                            Name = azureRole,
                            Code = azureRole.Substring(0, 2)
                        });
                }
                await _userManager.AddToRoleAsync(user, azureRole);
            }

            // ✅ NOW sign into Identity cookie
            await _signInManager.SignInAsync(user, isPersistent: false);

            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var user = await _signInManager.UserManager.GetUserAsync(User);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(Login));
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
        private async Task<IActionResult> PrepareInvalidView(LoginViewModel model, string error)
        {
            model.LoginError = error;
            ModelState.AddModelError(string.Empty, error);
            model.SetPassword = await featureManager.IsEnabledAsync(FeatureFlags.SHOW_USERS_ON_LOGIN);
            ViewData["Users"] = await loginService.GetUserSelectListAsync();
            return View(model);
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
    }
}