using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IValidationService
    {
        Task<bool> ValidateJwtToken(ApplicationDbContext context, HttpContext httpConext, string token);
    }
    public class ValidationService : IValidationService
    {
        private readonly IConfiguration config;

        public ValidationService(IConfiguration config)
        {
            this.config = config;
        }
        public async Task<bool> ValidateJwtToken(ApplicationDbContext context, HttpContext httpContext, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"])),
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                var userEmail = tokenHandler.ReadJwtToken(token).Claims.First(claim => claim.Type == ClaimTypes.Name).Value;
                var userLocation = tokenHandler.ReadJwtToken(token).Claims.First(claim => claim.Type == ClaimTypes.StreetAddress).Value;

                var user = context.ApplicationUser.FirstOrDefault(a => a.Email == userEmail);
                var userSessionAlive = new UserSessionAlive
                {
                    Updated = DateTime.Now,
                    ActiveUser = user,
                    CurrentPage = httpContext.Request.Path.Value
                };
                context.UserSessionAlive.Add(userSessionAlive);
                await context.SaveChangesAsync(null, false);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JWT validation failed: {ex.Message}");
                return false;
            }
        }
    }
}
