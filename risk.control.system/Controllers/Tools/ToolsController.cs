using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Common;

namespace risk.control.system.Controllers.Tools
{
    public class ToolsController(
            UserManager<ApplicationUser> userManager,
            ILoginService loginService,
            SignInManager<ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<ToolsController> logger,
            INotyfService notifyService) : Controller
    {
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ILoginService _loginService = loginService;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly ILogger<ToolsController> _logger = logger;
        private readonly INotyfService _notifyService = notifyService;

        [Authorize(Roles = GUEST.DISPLAY_NAME)]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Unauthorized");
            }

            var model = new ToolHubViewModel
            {
                FaceMatchRemaining = 5 - user.FaceMatchCount,
                OcrRemaining = 5 - user.OcrCount,
                PdfRemaining = 5 - user.PdfCount,
                DocumentAnalysisRemaining = 5 - user.DocumentAnalysisCount,
                Speech2TextRemaining = 5 - user.Speech2TextCount,
                Text2SpeechRemaining = 5 - user.Text2SpeechCount,
                ItrVerificationRemaining = 5 - user.ItrVerificationCount,
            };

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Try()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Try(OtpLoginModel model)
        {
            if (!ModelState.IsValid)
            {
                model.LoginError = "Bad Request";
                ModelState.AddModelError(string.Empty, "Bad Request");
                return View(model);
            }
            var otpRequest = new OtpRequest
            {
                CountryIsd = model.CountryIsd,
                MobileNumber = model.MobileNumber,
                BaseUrl = this._baseUrl // From the Controller's property
            };

            var success = await _loginService.SendOtpAsync(otpRequest);
            if (!success)
            {
                _notifyService.Warning($"Error to send Otp to Mobile <b>{model.CountryIsd}</b> <b>{model.MobileNumber} </b>. Try again.");
                return RedirectToAction(nameof(Try));
            }
            else
            {
                ModelState.Clear();
                _notifyService.Success($"Otp sent to Mobile <b>{model.CountryIsd}</b> <b>{model.MobileNumber} </b>. Please verify.");
                return View("VerifyOtp", model);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(OtpLoginModel model)
        {
            if (!ModelState.IsValid)
            {
                model.LoginError = "Bad Request";
                ModelState.AddModelError(string.Empty, "Bad Request");
                return View(model);
            }
            var result = await _loginService.VerifyAndLoginAsync(model);

            if (result.Success)
            {
                _notifyService.Success($"Welcome <b>{GUEST.DISPLAY_NAME}</b>, {result.Message}");
                return RedirectToAction(nameof(Index));
            }

            // If failed, return to the view with the error
            model.LoginError = result.Message;
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(OtpLoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data." });
            }

            var otpRequest = new OtpRequest
            {
                CountryIsd = model.CountryIsd,
                MobileNumber = model.MobileNumber,
                BaseUrl = this._baseUrl // From the Controller's property
            };
            // Call service logic
            var result = await _loginService.ResendOtpAsync(otpRequest);

            if (result.Success)
            {
                _notifyService.Success($"Otp resent to Mobile <b>{model.CountryIsd}</b> <b>{model.MobileNumber} </b>. Please verify.");
                return Json(new { success = true, message = result.Message });
            }
            else
            {
                _notifyService.Error($"Error to resend Otp to Mobile <b>{model.CountryIsd}</b> <b>{model.MobileNumber} </b>. Try again.");
                return Json(new { success = false, message = result.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LogoutGuest()
        {
            var user = await _signInManager.UserManager.GetUserAsync(User);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(Try));
        }
    }
}