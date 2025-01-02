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

namespace risk.control.system.Controllers.Mobile
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecureController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public SecureController(IConfiguration config)
        {
            this.configuration = config;
        }
        [AllowAnonymous]
        [HttpGet("jwt")]
        public IActionResult Jwt()
        {
            var message = GenerateJwtToken();
            return Ok(new { message });
        }

        // This endpoint requires JWT authentication.
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("jwt-api")]
        public IActionResult JwtApi()
        {
            var message = "This endpoint is secured with JWT";
            return Ok(new { message });
        }


        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpGet("cookie-api")]
        public IActionResult CookieApi()
        {
            return Ok(new { message = "This endpoint is secured with Cookie" });
        }


        private string GenerateJwtToken()
        {
            var key = Applicationsettings.HEXdATA;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, "creator@insurer.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "creator@insurer.com"),
        };

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(3),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
