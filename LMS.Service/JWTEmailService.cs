using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
//using LMS.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LMS.Service
{
    public class JwtEmailService : IJWTService

    {
        private readonly IConfiguration _config;

        public JwtEmailService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateEmailConfirmationToken(string userId, string email)
        {
            var jwtKey = _config["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT key configuration is missing.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expirationHours = 24;
            if (int.TryParse(_config["JwtSettings:EmailTokenExpirationHours"], out var hours))
                expirationHours = hours;

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal ValidateEmailConfirmationToken(string token)
        {
            var jwtKey = _config["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT key configuration is missing.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _config["JwtSettings:Issuer"],
                ValidAudience = _config["JwtSettings:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out _);

            foreach (var claim in principal.Claims)
            {
                Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }

            return principal;
        }
    }


}
    public interface IJWTService
    {
        string GenerateEmailConfirmationToken(string userId, string email);
        ClaimsPrincipal ValidateEmailConfirmationToken(string token);
    }

