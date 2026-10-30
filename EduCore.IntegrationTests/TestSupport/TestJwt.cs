using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduCoreAPI.Helpers;
using Microsoft.IdentityModel.Tokens;

namespace EduCore.IntegrationTests.TestSupport
{
    /// <summary>
    /// Mints real HS256 JWTs signed with the SAME secret the API host
    /// validates against (EnvConfig). This mirrors AuthController's token
    /// generation without going through the rate-limited login endpoint.
    /// </summary>
    public static class TestJwt
    {
        public static string Token(int userId, string email, params string[] roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Email, email)
            };

            foreach (string role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(EnvConfig.JwtSecretKey));

            var jwt = new JwtSecurityToken(
                issuer: EnvConfig.JwtIssuer,
                audience: EnvConfig.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(
                    key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public static string Bearer(int userId, string email, params string[] roles)
            => $"Bearer {Token(userId, email, roles)}";
    }
}
