using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Api
{
    public interface ITokenService
    {
        string GenerateJwtToken(ApplicationUser model);

        Task<RefreshToken> GenerateRefreshTokenAsync(string userId);
    }

    internal class TokenService(IConfiguration config, ApplicationDbContext context) : ITokenService
    {
        private readonly IConfiguration _config = config;
        private readonly ApplicationDbContext _context = context;

        public string GenerateJwtToken(ApplicationUser model)
        {
            // Fetch the signing key from configuration
            var key = _config["Jwt:Data"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Define claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, model.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, model.Email!),
                new Claim(ClaimTypes.Role, model.Role.ToString()!)
            };

            // Generate the token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
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
            _context.RefreshTokens.Add(new RefreshTokenEntity
            {
                Token = refreshToken.Token,
                UserId = refreshToken.UserId,
                ExpiryDate = refreshToken.ExpiryDate,
                IsRevoked = refreshToken.IsRevoked,
                IsUsed = refreshToken.IsUsed
            });

            await _context.SaveChangesAsync();

            return refreshToken;
        }
    }
}