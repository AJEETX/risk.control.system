using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using risk.control.system.AppConstant;
using static risk.control.system.AppConstant.Applicationsettings;
using risk.control.system.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.FeatureManagement;
using risk.control.system.Data;
using risk.control.system.Models.ViewModel;
using risk.control.system.Helpers;
using Microsoft.EntityFrameworkCore;

namespace risk.control.system.Controllers.Mobile
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]

    public class AuthController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ITokenService tokenService;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly INotificationService service;
        private readonly IAccountService accountService;
        private readonly ILogger _logger;
        private readonly IFeatureManager featureManager;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext _context;

        public AuthController(UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            INotificationService service,
            IConfiguration configuration,
            IAccountService accountService,
            ILogger<AccountController> logger,
            IFeatureManager featureManager,
            ISmsService SmsService,
            ApplicationDbContext context, ITokenService tokenService)
        {
            this.configuration = configuration;
            _userManager = userManager ?? throw new ArgumentNullException();
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.httpContextAccessor = httpContextAccessor;
            this.service = service;
            this.accountService = accountService;
            this._context = context;
            _logger = logger;
            this.featureManager = featureManager;
            smsService = SmsService;
            this.tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("AcceptCookies")]
        public IActionResult AcceptCookies()
        {
            Response.Cookies.Append("cookieConsent", "Accepted", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(265),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("analyticsCookies", true.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            Response.Cookies.Append("perfomanceCookies", true.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok(new { success = true, message = "Accept-All-Cookie consent saved" });
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("RevokeCookies")]
        public IActionResult RevokeCookies()
        {
            Response.Cookies.Append("cookieConsent", "Accepted", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365), // Persistent for 1 year
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            Response.Cookies.Append("analyticsCookies", false.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            Response.Cookies.Append("perfomanceCookies", false.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok(new { success = true, message = "Accept-only-required Cookie consent saved" });
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("SavePreferences")]
        public IActionResult SavePreferences([FromBody] CookiePreferences preferences)
        {
            if (preferences == null)
            {
                return BadRequest(new { success = false, message = "Invalid data received." });
            }

            Response.Cookies.Append("cookieConsent", "Accepted", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            Response.Cookies.Append("analyticsCookies", preferences.AnalyticsCookies.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            Response.Cookies.Append("perfomanceCookies", preferences.PerfomanceCookies.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new { success = true, message = "Cookie Preferences saved" });
        }

    }
    public class CookiePreferences
    {
        public bool AnalyticsCookies { get; set; }
        public bool PerfomanceCookies { get; set; }
    }
}