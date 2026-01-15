using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using risk.control.system.AppConstant;
using risk.control.system.Data;
using risk.control.system.Helpers;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Mobile
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecureController : ControllerBase
    {
        private readonly ITokenService tokenService;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IPdfGenerativeService pdfGenerativeService;
        private readonly IPhoneService phoneService;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IFeatureManager featureManager;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext _context;
        private readonly string baseUrl;
        public SecureController(UserManager<Models.ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IPdfGenerativeService generateService,
            IPhoneService phoneService,
            SignInManager<Models.ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            IFeatureManager featureManager,
            ISmsService SmsService,
            ApplicationDbContext context,
            ITokenService tokenService)
        {
            _userManager = userManager ?? throw new ArgumentNullException();
            this.webHostEnvironment = webHostEnvironment;
            this.pdfGenerativeService = generateService;
            this.phoneService = phoneService;
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this._context = context;
            this.featureManager = featureManager;
            smsService = SmsService;
            this.tokenService = tokenService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [AllowAnonymous]
        [HttpPost("agent-login")]
        public async Task<IActionResult> Login(AgentLoginModel model)
        {
            if (!ModelState.IsValid || !model.Email.ValidateEmail())
            {
                return BadRequest("Invalid login attempt.");
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return BadRequest("User account locked out.");
                }
                else
                {
                    return BadRequest("Invalid login attempt.");
                }
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest("Invalid login attempt.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles == null || roles.Count == 0)
            {
                return BadRequest("Invalid login attempt.");
            }

            var vendorUser = await _context.ApplicationUser.FirstOrDefaultAsync(u => u.Email == model.Email && !u.Deleted && u.Role == AppRoles.AGENT);
            if (vendorUser == null)
            {
                return BadRequest("Invalid login attempt.");
            }
            bool vendorIsActive = false;
            vendorIsActive = await _context.Vendor.AnyAsync(c => c.VendorId == vendorUser.VendorId && c.Status == Models.VendorStatus.ACTIVE);
            if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED) && vendorIsActive)
            {
                vendorIsActive = !string.IsNullOrWhiteSpace(user.MobileUId);
            }

            if (!vendorIsActive || !user.Active)
            {
                return BadRequest("Invalid login attempt.");
            }

            var token = tokenService.GenerateJwtToken(user);
            var refreshToken = await tokenService.GenerateRefreshTokenAsync(user.Email);

            return Ok(new TokenResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15) // Matches access token lifetime
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.IsUsed)
                return Unauthorized("Invalid or expired refresh token.");

            if (tokenEntity.ExpiryDate <= DateTime.UtcNow)
                return Unauthorized("Refresh token has expired.");

            var user = await _userManager.FindByIdAsync(tokenEntity.UserId);
            if (user == null)
                return Unauthorized("User not found.");

            // Mark the current refresh token as used
            tokenEntity.IsUsed = true;
            _context.RefreshTokens.Update(tokenEntity);
            await _context.SaveChangesAsync();

            // Generate new tokens
            var newAccessToken = tokenService.GenerateJwtToken(user);
            var newRefreshToken = await tokenService.GenerateRefreshTokenAsync(user.Email);

            return Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15) // Matches access token lifetime
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> RevokeToken([FromBody] string refreshToken)
        {
            var tokenEntity = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (tokenEntity == null)
                return NotFound("Token not found.");

            tokenEntity.IsRevoked = true;
            _context.RefreshTokens.Update(tokenEntity);
            await _context.SaveChangesAsync();
            return Ok("Token revoked successfully.");
        }

        [AllowAnonymous]
        [HttpGet("test-2-get-jwt-token")]
        public async Task<IActionResult> Jwt(string username = "agent@verify.com")
        {
            var user = await _userManager.FindByEmailAsync(username);
            if (user == null)
            {
                return BadRequest("Invalid login attempt.");
            }

            var token = tokenService.GenerateJwtToken(user);
            return Ok(new { token });
        }
        // This endpoint requires JWT authentication.
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("test-2-access-secure-api")]
        public IActionResult JwtApi()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var latlong = User.FindFirst(ClaimTypes.StreetAddress)?.Value;
            var message = $"This endpoint is secured with JWT, for username : {username}, role: {role}, latLong: {latlong}";
            return Ok(new { message });
        }

        [AllowAnonymous]
        [HttpGet("test-sms")]
        public async Task<IActionResult> Sms(string countryCode = "au", string mobile = "61432854196", string message = "Testing by icheckify")
        {
            string msg = $"Dear {mobile} user,\n\n" +
                             $"iCheckify: {message}\n\n" +
                             $"Thanks\n{baseUrl}";
            var response = await smsService.SendSmsAsync(countryCode, mobile, msg);
            return Ok(new { message = response });
        }

        [AllowAnonymous]
        [HttpGet("validate-mobile-number")]
        public async Task<IActionResult> ValidatePhoneNumber(string phoneNumber = "+61432854196")
        {
            var result = await phoneService.ValidateAsync(phoneNumber);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("is-mobile-number")]
        public IActionResult IsValidMobileNumber(string phoneNumber = "+61432854196", string country = "61")
        {
            var result = phoneService.IsValidMobileNumber(phoneNumber, country);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("pdf")]
        public async Task<IActionResult> Pdf(long id = 1, string currentUserEmail = "assessor@insurer.com")
        {
            try
            {
                var reportPathTask = await pdfGenerativeService.GeneratePdf(id, currentUserEmail);

                var reportFilename = "report" + reportPathTask.Id + ".pdf";

                var memory = new MemoryStream();
                using var stream = new FileStream(reportPathTask.InvestigationReport.PdfReportFilePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                return File(memory, "application/pdf", reportFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Internal server error");
            }
        }

        [AllowAnonymous]
        [HttpGet("old-pdf")]
        public async Task<IActionResult> OldPdf(long id = 1, string currentUserEmail = "assessor@insurer.com")
        {
            try
            {
                var reportFilename = await pdfGenerativeService.Generate(id, currentUserEmail);

                var ReportFilePath = Path.Combine(webHostEnvironment.WebRootPath, "report", reportFilename);
                var memory = new MemoryStream();
                using var stream = new FileStream(ReportFilePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                return File(memory, "application/pdf", reportFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}