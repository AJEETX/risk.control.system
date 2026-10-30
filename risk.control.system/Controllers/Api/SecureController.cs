using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Api;
using risk.control.system.Services.Common;
using risk.control.system.Services.Report;

namespace risk.control.system.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecureController(UserManager<ApplicationUser> userManager,
            IProcessImageService processImageService,
            IAmazonApiService amazonApiService,
            IWebHostEnvironment webHostEnvironment,
            IPdfReportService pdfReportService,
            IPhoneService phoneService,
            SignInManager<ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            IFeatureManager featureManager,
            ISmsService smsService,
            ApplicationDbContext context,
            ITokenService tokenService) : ControllerBase
    {
        private readonly string _baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent()}{httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent()}";
        private readonly ITokenService _tokenService = tokenService;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IProcessImageService _processImageService = processImageService;
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly IWebHostEnvironment _env = webHostEnvironment;
        private readonly IPdfReportService _pdfReportService = pdfReportService;
        private readonly IPhoneService _phoneService = phoneService;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly ISmsService _smsService = smsService;
        private readonly ApplicationDbContext _context = context;

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
            if (await _featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED) && vendorIsActive)
            {
                vendorIsActive = !string.IsNullOrWhiteSpace(user.MobileUId);
            }
            if (!vendorIsActive || !user.Active)
            {
                return BadRequest("Invalid login attempt.");
            }
            var token = _tokenService.GenerateJwtToken(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Email!);
            return Ok(new TokenResponse { AccessToken = token, RefreshToken = refreshToken.Token, ExpiresAt = DateTime.UtcNow.AddMinutes(15) });
        }

        //[AllowAnonymous]
        //[HttpPost("refresh-token")]
        //public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        //{
        //    var tokenEntity = await _context.RefreshTokens
        //        .FirstOrDefaultAsync(t => t.Token == refreshToken);

        //    if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.IsUsed)
        //        return Unauthorized("Invalid or expired refresh token.");

        //    if (tokenEntity.ExpiryDate <= DateTime.UtcNow)
        //        return Unauthorized("Refresh token has expired.");

        //    var user = await _userManager.FindByIdAsync(tokenEntity.UserId);
        //    if (user == null)
        //        return Unauthorized("User not found.");

        //    // Mark the current refresh token as used
        //    tokenEntity.IsUsed = true;
        //    _context.RefreshTokens.Update(tokenEntity);
        //    await _context.SaveChangesAsync();

        //    // Generate new tokens
        //    var newAccessToken = tokenService.GenerateJwtToken(user);
        //    var newRefreshToken = await tokenService.GenerateRefreshTokenAsync(user.Email);

        //    return Ok(new TokenResponse
        //    {
        //        AccessToken = newAccessToken,
        //        RefreshToken = newRefreshToken.Token,
        //        ExpiresAt = DateTime.UtcNow.AddMinutes(15) // Matches access token lifetime
        //    });
        //}

        //[Authorize]
        //[HttpPost("logout")]
        //public async Task<IActionResult> RevokeToken([FromBody] string refreshToken)
        //{
        //    var tokenEntity = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        //    if (tokenEntity == null)
        //        return NotFound("Token not found.");

        //    tokenEntity.IsRevoked = true;
        //    _context.RefreshTokens.Update(tokenEntity);
        //    await _context.SaveChangesAsync();
        //    return Ok("Token revoked successfully.");
        //}

        [AllowAnonymous]
        [HttpGet("test-2-get-jwt-token")]
        public async Task<IActionResult> Jwt(string username = "agent@checker.com")
        {
            var user = await _userManager.FindByEmailAsync(username);
            if (user == null)
            {
                return BadRequest("Invalid login attempt.");
            }

            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new { token });
        }

        // This endpoint requires JWT authentication.
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("test-2-access-secure-api")]
        public IActionResult JwtApi()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var message = $"This endpoint is secured with JWT, for username : {username}, role: {role}";
            return Ok(new { message });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("test-sms")]
        public async Task<IActionResult> Sms(string countryCode = "au", string mobile = "61432854196", string message = "Testing")
        {
            string msg = $"Dear {mobile} user,\n\n" +
                             $"{message}\n\n" +
                             $"Thanks\n{_baseUrl}";
            var response = await _smsService.SendSmsAsync(countryCode, mobile, msg);
            return Ok(new { message = response });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("validate-mobile-number")]
        public async Task<IActionResult> ValidatePhoneNumber(string phoneNumber = "+61432854196")
        {
            var result = await _phoneService.ValidateAsync(phoneNumber);
            return Ok(result);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("is-mobile-number")]
        public async Task<IActionResult> IsValidMobileNumber(string phoneNumber = "+61432854196", string country = "61")
        {
            var result = await _phoneService.IsValidMobileNumberAsync(phoneNumber, country);
            return Ok(result);
        }

        //[AllowAnonymous]
        //[HttpGet("pdf-quest")]
        //public async Task<IActionResult> Pdf(long id = 1, string currentUserEmail = "assessor@insurer.com")
        //{
        //    try
        //    {
        //        var reportPathTask = await pdfGenerativeService.GeneratePdf(id, currentUserEmail);

        //        var reportFilename = "report" + reportPathTask.Id + ".pdf";

        //        var memory = new MemoryStream();
        //        using var stream = new FileStream(reportPathTask.InvestigationReport.PdfReportFilePath, FileMode.Open);
        //        await stream.CopyToAsync(memory);
        //        memory.Position = 0;
        //        return File(memory, "application/pdf", reportFilename);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        return StatusCode(500, "Internal server error");
        //    }
        //}

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpGet("assessor-report")]
        public async Task<IActionResult> AssessorReport(long id = 1, string currentUserEmail = "assessor@insurer.com")
        {
            try
            {
                var caseNumber = await _pdfReportService.Generate(id, currentUserEmail);
                string ReportFilePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.DOCUMENT, CONSTANTS.CASE, caseNumber, CONSTANTS.REPORT_FILENAME));
                var memory = new MemoryStream();
                using var stream = new FileStream(ReportFilePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                return File(memory, "application/pdf", CONSTANTS.REPORT_FILENAME);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Internal server error");
            }
        }

        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [AllowAnonymous]
        [HttpGet("agency-report")]
        public async Task<IActionResult> AgencyReport(long id = 1)
        {
            try
            {
                var caseNumber = await _pdfReportService.GenerateAgencyReport(id);
                string agencyReportFilePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, CONSTANTS.DOCUMENT, CONSTANTS.CASE, caseNumber, CONSTANTS.REPORT, CONSTANTS.AGENCY_REPORT_FILENAME));
                var memory = new MemoryStream();
                await using var stream = new FileStream(agencyReportFilePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                return File(memory, "application/pdf", CONSTANTS.AGENCY_REPORT_FILENAME);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Internal server error");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AGENT.DISPLAY_NAME}")]
        [HttpPost("watermark-image")]
        public async Task<IActionResult> WatermarkImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            try
            {
                await using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var watermarkedBytes = _processImageService.CompressAndWaterMarkImage(imageBytes, 80, "VERIFIED", 0f);
                return File(watermarkedBytes, "image/*", $"watermarked_{file.FileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error watermarking image {FileName}", ex.Message);
                return StatusCode(500, "An error occurred while processing the image.");
            }
        }
    }
}