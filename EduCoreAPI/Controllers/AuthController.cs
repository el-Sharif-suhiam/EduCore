using Common.Enums;
using EduCore_BusinessLayer;
using EduCoreAPI.Helpers;
using EduCoreAPI.Helpers.Models.RequestModels;
using EduCoreAPI.Helpers.Models.ResponeModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EduCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
       
        // This endpoint handles user login.
        // It verifies credentials and returns a JWT token if login succeeds.
        [HttpPost("login")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await clsUser.Find(request.Email);

            
            bool isValidPassword = user.VerifyPassword(request.Password);


            // If the password does not match the stored hash,
            // return 401 Unauthorized.
            if (!isValidPassword)
                throw new UnauthorizedAccessException("Invalid credentials");



            // Step 3: Create claims that represent the authenticated user's identity.
            // These claims will be embedded inside the JWT.
        //    Claim[] claims = new Claim[]
        //    {
        //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

        //    new Claim(ClaimTypes.Email, user.Email),

        //    // Role (Student, Instructor or Admin) used later for authorization

        //    new Claim(ClaimTypes.Role, user.Role)
        //};
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Add all user roles
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            
            DotNetEnv.Env.Load();

            // This key must match the key used in JWT validation middleware.
            // Single source of truth: EnvConfig (Program.cs uses the same).
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(EnvConfig.JwtSecretKey));


            // This specifies the algorithm used to sign the token.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


            // Create the JWT token.
            // The token includes issuer, audience, claims, expiration, and signature.
            var token = new JwtSecurityToken(
                issuer: EnvConfig.JwtIssuer,
                audience: EnvConfig.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            // Create refresh token (random)
            var refreshToken = GenerateRefreshToken();

            // Store refresh token securely (hash + expiry + not revoked)
            user.SetRefreshToken(refreshToken);
            user.SetRefreshExpiredAt(DateTime.UtcNow.AddDays(3));

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            bool isUpdated = await user.Save(ip,userAgent);
            if (!isUpdated)
                throw new Exception("Something went wrong after trying to update refresh token");

            await clsAudit.LogAsync(
           user.Id,
           enAuditActionType.Login,
           "User",
           user.Id,
           $"User Logged in:",
           ip,
           userAgent);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });

            
        }


        [HttpPost("refresh")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var user = await clsUser.Find(request.Email);

            if (user.RefreshTokenRevokedAt != null)
                throw new UnauthorizedAccessException("Refresh token is revoked");

            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token expired");

            bool refreshValid = user.VerifyRefreshToken(request.RefreshToken);
            if (!refreshValid)
                throw new UnauthorizedAccessException("Invalid refresh token");

            // Issue NEW access token (same claims & signing settings as login)
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

                new Claim(ClaimTypes.Email, user.Email)
            };

            // Add all user roles
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            DotNetEnv.Env.Load();
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(EnvConfig.JwtSecretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: EnvConfig.JwtIssuer,
                audience: EnvConfig.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            var newAccessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Rotation: replace refresh token
            var newRefreshToken = GenerateRefreshToken();


            // Store refresh token securely (hash + expiry + not revoked)
            user.SetRefreshToken(newRefreshToken);
            user.SetRefreshExpiredAt(DateTime.UtcNow.AddDays(3));
            user.SetRefreshRevokedAt(null);

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            

            bool isUpdated = await user.Save(ip,userAgent);
            if (!isUpdated)
                throw new Exception("Something went wrong after trying to update refresh token");

            return Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var user = await clsUser.Find(request.Email);


            if (user == null)
                return Ok(); // Do not reveal if user exists

            bool refreshValid = user.VerifyRefreshToken(request.RefreshToken);

            if (!refreshValid)
                return Ok();

            user.SetRefreshRevokedAt(DateTime.UtcNow);
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            bool isUpdated = await user.Save(ip,userAgent);
            if (!isUpdated)
                throw new Exception("Something went wrong after trying to update refresh token");


            await clsAudit.LogAsync(
           user.Id,
           enAuditActionType.Logout,
           "User",
           user.Id,
           $"User Logged out:",
           ip,
           userAgent);
            return Ok("Logged out successfully");
        }
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }


    }
}
