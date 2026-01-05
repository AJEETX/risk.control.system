using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;
namespace risk.control.system.Controllers.Tools
{

    public class ToolController : Controller
    {
        private readonly IMemoryCache cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoginService loginService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly INotyfService notifyService;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext _context;
        private readonly string BaseUrl;

        public ToolController(
            IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            ILoginService loginService,
            SignInManager<ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            ILogger<AccountController> logger,
            INotyfService notifyService,
            ISmsService SmsService,
            ApplicationDbContext context)
        {
            this.cache = cache;
            _userManager = userManager ?? throw new ArgumentNullException();
            this.loginService = loginService;
            _signInManager = signInManager ?? throw new ArgumentNullException();
            _context = context;
            _logger = logger;
            this.notifyService = notifyService;
            smsService = SmsService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }
        [Authorize(Roles = GUEST.DISPLAY_NAME)]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Try));

            var model = new ToolHubViewModel
            {
                FaceMatchRemaining = 5 - user.FaceMatchCount,
                OcrRemaining = 5 - user.OcrCount,
                PdfRemaining = 5 - user.PdfCount
            };

            return View(model);
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Try()
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
                BaseUrl = this.BaseUrl // From the Controller's property
            };

            var success = await loginService.SendOtpAsync(otpRequest);
            if (!success)
            {
                notifyService.Warning($"Error to send Otp to Mobile <b>{model.CountryIsd}</b> <b>{model.MobileNumber} </b>. Try again.");
                return RedirectToAction(nameof(Try));
            }
            return RedirectToAction(nameof(VerifyOtp), new
            {
                isd = model.CountryIsd,
                mobileNumber = model.MobileNumber.TrimStart('0')
            });
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyOtp(string isd, string mobileNumber)
        {
            var model = new OtpLoginModel
            {
                CountryIsd = isd,
                MobileNumber = mobileNumber
            };
            return View(model);
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
            var result = await loginService.VerifyAndLoginAsync(model.CountryIsd, model.MobileNumber, model.UserEnteredOtp);

            if (result.Success)
            {
                notifyService.Success($"Welcome <b>{GUEST.DISPLAY_NAME}</b>, {result.Message}");
                return RedirectToAction(nameof(Index));
            }

            // If failed, return to the view with the error
            model.LoginError = result.Message;
            return View(model);
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string isd, string mobileNumber)
        {
            if (string.IsNullOrEmpty(mobileNumber) || string.IsNullOrEmpty(isd))
            {
                return BadRequest(new { success = false, message = "Invalid data." });
            }
            if (string.IsNullOrEmpty(mobileNumber) || string.IsNullOrEmpty(isd))
            {
                return Json(new { success = false, message = "Invalid mobile details." });
            }
            var otpRequest = new OtpRequest
            {
                CountryIsd = isd,
                MobileNumber = mobileNumber,
                BaseUrl = this.BaseUrl // From the Controller's property
            };
            // Call service logic
            var result = await loginService.ResendOtpAsync(otpRequest);

            if (result.Success)
            {
                return Json(new { success = true, message = result.Message });
            }

            return Json(new { success = false, message = result.Message });
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
