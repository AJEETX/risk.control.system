using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using risk.control.system.AppConstant;
using risk.control.system.Models;

namespace risk.control.system.Services.Common
{
    public interface IJwtService
    {
        Task<bool> ValidateJwtToken(ApplicationDbContext context, HttpContext httpConext, string token);
    }

    internal class JwtService : IJwtService
    {
        private readonly IConfiguration config;
        private readonly ILogger<JwtService> logger;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            this.config = config;
            this.logger = logger;
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Data"])),
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                var userEmail = tokenHandler.ReadJwtToken(token).Claims.First(claim => claim.Type == ClaimTypes.Name).Value;
                var userRole = tokenHandler.ReadJwtToken(token).Claims.First(claim => claim.Type == ClaimTypes.Role).Value;
                var appRole = (AppRoles)Enum.Parse(typeof(AppRoles), userRole);
                var user = await context.ApplicationUser.FirstOrDefaultAsync(a => a.Email == userEmail && a.Role == appRole);
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
                logger.LogError(ex, "Error occurred.");
                throw;
            }
        }
    }
}