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
using risk.control.system.Services;

namespace risk.control.system.Controllers
{
    public class AccountController : Controller
    {
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
            var credentialModelJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                email = user.Email,
                currentPassword = user.Password
            });

            await Response.WriteAsync($"data: CREDENTIAL|{credentialModelJson}\n");
            await Response.Body.FlushAsync(cancellationToken);
            await Task.Delay(1000, cancellationToken); // Small delay to ensure UI updates first

            // Now, stream messages one by one
            var messages = new List<string>
            {
                $"Welcome ! {user.Email} First time user.",
                "Please update credential to continue.",
                "Remember credential for later."
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
        public IActionResult AzureLogin(string returnUrl = "/")
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }
        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
        //{
        //    var authResult = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);

        //    if (!authResult.Succeeded || authResult.Principal == null)
        //    {
        //        return RedirectToAction(nameof(Login));
        //    }

        //    try
        //    {
        //        var user = await loginService.CreateOrUpdateExternalUserAsync(authResult.Principal);

        //        if (user == null)
        //            return View("Error", "Email not received from Azure");

        //        await _signInManager.SignInAsync(user, isPersistent: false);

        //        return LocalRedirect(returnUrl);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "External login failed for user.");

        //        return View("Error", new ErrorViewModel
        //        {
        //            Message = ex.Message,
        //            RequestId = HttpContext.TraceIdentifier
        //        });
        //    }
        //}

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
            try
            {
                var smsResult = await accountService.ForgotPassword(input.Email, input.Mobile, input.CountryId);

                if (smsResult == null)
                {
                    model = await CreateDefaultForgotPasswordModel(input?.Email);
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
                _logger.LogError(ex, "Error in Forgot Password");
                throw;
            }
            
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