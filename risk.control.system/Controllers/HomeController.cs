using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;
namespace risk.control.system.Controllers
{

    [Authorize(Roles = GUEST.DISPLAY_NAME)]
    public class HomeController : Controller
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

        public HomeController(
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
            this._context = context;
            _logger = logger;
            this.notifyService = notifyService;
            smsService = SmsService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            BaseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }
        public IActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Otp()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Otp(OtpLoginModel model)
        {
            var otp = new Random().Next(1000, 9999).ToString();
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)).SetSize(1);

            cache.Set($"{model.CountryIsd.TrimStart('+')}{model.MobileNumber.TrimStart('0')}", otp, cacheOptions);

            var country = await _context.Country.FirstOrDefaultAsync(c => c.ISDCode.ToString() == model.CountryIsd.TrimStart('+'));
            var message = $"Hi user {model.CountryIsd} {model.MobileNumber.TrimStart('0')}\n" +
                                     $"Your code is {otp}\n" +
                                     $"{BaseUrl}";
            var smsResponse = await smsService.SendSmsAsync(country.Code, model.CountryIsd + model.MobileNumber.TrimStart('0'), message);
            if (!string.IsNullOrWhiteSpace(smsResponse))
            {
                return RedirectToAction("VerifyOtp", new
                {
                    isd = model.CountryIsd,
                    mobileNumber = model.MobileNumber.TrimStart('0')
                });
            }
            else
            {
                notifyService.Warning($"Error to send Otp to Mobile <b>{model.CountryIsd}</b> <b>{model.MobileNumber} </b>. Try again.");
                return RedirectToAction(nameof(Otp));
            }
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
            //notifyService.Success($"Otp sent to {isd} {mobileNumber}");
            return View(model);
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string mobileNumber, string userEnteredOtp, string isd)
        {
            if (!ModelState.IsValid)
            {
                var model = new OtpLoginModel();
                model.LoginError = "Bad Request";
                ModelState.AddModelError(string.Empty, "Bad Request");
                return View(model);
            }
            string cacheKey = $"{isd.TrimStart('+')}{mobileNumber.TrimStart('0')}";
            string? correctOtp = string.Empty;
            if (!cache.TryGetValue(cacheKey, out correctOtp))
            {
                ModelState.AddModelError("", "Invalid OTP");
                return View();
            }
            if (correctOtp != userEnteredOtp.Trim())
            {
                ModelState.AddModelError("", "Invalid OTP");
                return View();
            }
            // OTP is valid, proceed with login or registration
            var username = isd.TrimStart('+') + mobileNumber.TrimStart('0') + "@icheckify.co.in";
            var userExist = await _userManager.FindByNameAsync(username);
            if (userExist != null)
            {
                await loginService.SignInWithTimeoutAsync(userExist);
                notifyService.Success($"Welcome <b>{GUEST.DISPLAY_NAME}</b>, Login successful");
                return RedirectToAction("Index", "Home");
            }
            var country = await _context.Country.FirstOrDefaultAsync(c => c.ISDCode.ToString() == isd.TrimStart('+'));
            var apppUser = new ApplicationUser
            {
                FirstName = "Guest",
                LastName = "Guest",
                PhoneNumber = mobileNumber.TrimStart('0'),
                UserName = username,
                Email = username,
                CountryId = country.CountryId
            };
            var result = await _userManager.CreateAsync(apppUser);
            if (!result.Succeeded)
            {
                return PrepareGuestInvalidView(result.Errors.FirstOrDefault());

            }
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(apppUser, AppRoles.GUEST.ToString());
                notifyService.Success($"Welcome <b>{GUEST.DISPLAY_NAME}</b>, Login successful");
                await loginService.SignInWithTimeoutAsync(apppUser);
            }

            return RedirectToAction("Index", "Home");
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

            var newOtp = new Random().Next(1000, 9999).ToString();
            var cacheKey = $"{isd.TrimStart('+')}{mobileNumber.TrimStart('0')}";
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSize(1);
            cache.Set(cacheKey, newOtp, cacheOptions);
            var country = await _context.Country.FirstOrDefaultAsync(c => c.ISDCode.ToString() == isd.TrimStart('+'));
            var message = $"Hi user {isd} {mobileNumber.TrimStart('0')}\n" +
                                     $"Your code is {newOtp}\n" +
                                     $"{BaseUrl}";
            var smsResponse = await smsService.SendSmsAsync(country.Code, isd + mobileNumber.TrimStart('0'), message);
            if (!string.IsNullOrWhiteSpace(smsResponse))
            {
                return Ok(new { success = true, message = "OTP Resent Successfully!" });
            }
            else
            {
                return Ok(new { success = false, message = "Failed to resend OTP. Please try again later." });
            }
        }
        private IActionResult PrepareGuestInvalidView(IdentityError error)
        {
            var model = new OtpLoginModel();
            model.LoginError = error.Description;
            ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LogoutGuest()
        {
            var user = await _signInManager.UserManager.GetUserAsync(User);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(Otp));
        }
    }
}
