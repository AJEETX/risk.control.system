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
        private readonly IPdfGenerativeService pdfGenerativeService;
        private readonly ITokenService tokenService;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly SignInManager<Models.ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly INotificationService service;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger _logger;
        private readonly IFeatureManager featureManager;
        private readonly ISmsService smsService;
        private readonly ApplicationDbContext _context;
        private readonly string baseUrl;
        public SecureController(UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
             IHttpContextAccessor httpContextAccessor,
            INotificationService service,
            IPdfGenerativeService pdfGenerativeService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AccountController> logger,
            IFeatureManager featureManager,
            ISmsService SmsService,
            ApplicationDbContext context,
            ITokenService tokenService)
        {
            this.pdfGenerativeService = pdfGenerativeService;
            _userManager = userManager ?? throw new ArgumentNullException();
            _signInManager = signInManager ?? throw new ArgumentNullException();
            this.httpContextAccessor = httpContextAccessor;
            this.service = service;
            this.webHostEnvironment = webHostEnvironment;
            this._context = context;
            _logger = logger;
            this.featureManager = featureManager;
            smsService = SmsService;
            this.tokenService = tokenService;
            var host = httpContextAccessor?.HttpContext?.Request.Host.ToUriComponent();
            var pathBase = httpContextAccessor?.HttpContext?.Request.PathBase.ToUriComponent();
            baseUrl = $"{httpContextAccessor?.HttpContext?.Request.Scheme}://{host}{pathBase}";
        }

        [AllowAnonymous]
        [HttpPost("agent-login")]
        public async Task<IActionResult> Login(CancellationToken ct, AgentLoginModel model)
        {
            var ipAddress = HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR") ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var ipAddressWithoutPort = ipAddress?.Split(':')[0];

            if (!ModelState.IsValid || !model.Email.ValidateEmail())
            {
                return BadRequest("Invalid login attempt.");
            }
            var email = System.Web.HttpUtility.HtmlEncode(model.Email);
            var pwd = System.Web.HttpUtility.HtmlEncode(model.Password);
            var result = await _signInManager.PasswordSignInAsync(email, pwd, false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                var roles = await _userManager.GetRolesAsync(user);
                if (roles != null && roles.Count > 0)
                {
                    var vendorUser = _context.VendorApplicationUser.FirstOrDefault(u => u.Email == email && !u.Deleted && u.Role == AppRoles.AGENT);

                    bool vendorIsActive = false;
                    vendorIsActive = _context.Vendor.Any(c => c.VendorId == vendorUser.VendorId && c.Status == Models.VendorStatus.ACTIVE);
                    if (await featureManager.IsEnabledAsync(FeatureFlags.ONBOARDING_ENABLED) && vendorIsActive)
                    {
                        vendorIsActive = !string.IsNullOrWhiteSpace(user.MobileUId);
                    }
                    if (vendorIsActive && user.Active)
                    {
                        var isAuthenticated = HttpContext.User.Identity.IsAuthenticated;
                        string message = string.Empty;
                        var ipApiResponse = await service.GetAgentIp(ipAddressWithoutPort, ct, "login-success", model.Email, isAuthenticated, model.Latlong);

                        if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) && user?.Email != null)
                        {
                            var admin = _context.ApplicationUser.Include(c => c.Country).FirstOrDefault(u => u.IsSuperAdmin);
                            if (admin != null)
                            {
                                message = $"Dear {admin.Email}";
                                message += $"                                       ";
                                message += $"                       ";
                                message += $"User {user.Email} logged in from IP address {ipApiResponse.query}";
                                message += $"                                       ";
                                message += $"Thanks                                         ";
                                message += $"                                       ";
                                message += $"                                       ";
                                message += $"{baseUrl}";
                                try
                                {
                                    await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + admin.PhoneNumber, message);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            message += $"                                       ";
                            message += $"                       ";
                            message += $"User {user.Email} logged in from IP address {ipApiResponse.query}";
                            message += $"                                       ";
                            message += $"Thanks                                         ";
                            message += $"                                       ";
                            message += $"                                       ";
                            message += $"{baseUrl}";
                            try
                            {
                                await smsService.DoSendSmsAsync("+" + admin.Country.ISDCode + admin.PhoneNumber, message);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                        model.Role = vendorUser.Role.ToString();
                        var token = tokenService.GenerateJwtToken(model);
                        var refreshToken = await tokenService.GenerateRefreshTokenAsync(model.Email);

                        return Ok(new TokenResponse
                        {
                            AccessToken = token,
                            RefreshToken = refreshToken.Token,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(15) // Matches access token lifetime
                        });
                    }
                }

                return BadRequest();
            }
            else if (result.IsLockedOut)
            {
                return BadRequest("User account locked out.");
            }
            else
            {
                return BadRequest("Invalid login attempt.");
            }
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
            var model = new AgentLoginModel
            {
                Email = user.Email,
                Role = user.Role.ToString()
            };
            // Generate new tokens
            var newAccessToken = tokenService.GenerateJwtToken(model);
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
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (tokenEntity == null)
                return NotFound("Token not found.");

            tokenEntity.IsRevoked = true;
            _context.RefreshTokens.Update(tokenEntity);
            await _context.SaveChangesAsync();

            return Ok("Token revoked successfully.");
        }

        [AllowAnonymous]
        [HttpGet("test-2-get-jwt-token")]
        public async Task<IActionResult> Jwt(string username = "agentx@verify.com")
        {
            var model = new AgentLoginModel
            {
                Email = username,
                Role = $"{AGENT.DISPLAY_NAME}"
            };
            var token = tokenService.GenerateJwtToken(model);
            await Task.Delay(10);
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
        public async Task<IActionResult> Sms(string mobile = "61432854196", string message = "Testing by icheckify")
        {
            string msg = $"Dear {mobile} user,\n\n" +
                             $"iCheckify: {message}\n\n" +
                             $"Thanks\n{baseUrl}";
            var response = await SmsService.SendSmsAsync(mobile, msg);
            return Ok(new { message = response });
        }

        [AllowAnonymous]
        [HttpGet("pdf")]
        public async Task<IActionResult> Pdf(long id = 1, string currentUserEmail = "assessor@insurer.com")
        {
            try
            {
                var reportFilename = await pdfGenerativeService.Generate(id, currentUserEmail);

                var ReportFilePath = Path.Combine(webHostEnvironment.WebRootPath, "report", reportFilename);
                var memory = new MemoryStream();
                using var stream = new FileStream(ReportFilePath, FileMode.Open);
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                //notifyService.Success($"Policy {claim.PolicyDetail.ContractNumber} Report download success !!!");
                return File(memory, "application/pdf", reportFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing case report");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}