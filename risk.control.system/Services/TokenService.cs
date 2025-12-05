using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ITokenService
    {
        string GenerateJwtToken(AgentLoginModel model);
        Task<RefreshToken> GenerateRefreshTokenAsync(string userId);
    }
    public class TokenService : ITokenService
    {
        private readonly IConfiguration config;
        private readonly ApplicationDbContext context;

        public TokenService(IConfiguration config, ApplicationDbContext context)
        {
            this.config = config;
            this.context = context;
        }
        public string GenerateJwtToken(AgentLoginModel model)
        {
            // Fetch the signing key from configuration
            var key = config["Jwt:Data"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Define claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, model.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, model.Email),
                new Claim(ClaimTypes.StreetAddress, model.Latlong),
                new Claim(ClaimTypes.Role, model.Role)
            };

            // Generate the token
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials);

            // Return the serialized token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
        {
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(1), // Set token lifetime (e.g., 7 days)
                IsRevoked = false,
                IsUsed = false
            };

            // Save to the database
            context.RefreshTokens.Add(new RefreshTokenEntity
            {
                Token = refreshToken.Token,
                UserId = refreshToken.UserId,
                ExpiryDate = refreshToken.ExpiryDate,
                IsRevoked = refreshToken.IsRevoked,
                IsUsed = refreshToken.IsUsed
            });

            await context.SaveChangesAsync();

            return refreshToken;
        }

    }
}
