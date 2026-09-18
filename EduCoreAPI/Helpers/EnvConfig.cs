using System.Text;

namespace EduCoreAPI.Helpers
{
    /// <summary>
    /// Centralised, fail-fast access to environment configuration.
    ///
    /// Why: token generation (AuthController) and validation
    /// (Program.cs) MUST share one issuer/audience/secret source or
    /// they can silently drift apart. Missing/weak secrets should also
    /// kill the API at STARTUP, not on the first login attempt.
    ///
    /// Values are read live from the process environment (DotNetEnv
    /// has already loaded EduCoreAPI/.env before these are touched).
    /// </summary>
    public static class EnvConfig
    {
        public const string DefaultIssuer = "EduCoreApi";
        public const string DefaultAudience = "EduCoreApiUsers";

        /// <summary>Reads a required setting; throws at startup when absent.</summary>
        public static string Required(string name)
            => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException(
                $"Required env '{name}' is not set.");

        /// <summary>Reads a setting, falling back to <paramref name="fallback"/> when absent.</summary>
        public static string Or(string name, string fallback)
            => Environment.GetEnvironmentVariable(name) ?? fallback;

        /// <summary>Parses a comma-separated env list (trims entries, drops empties).</summary>
        public static string[] List(string name, string fallback)
            => (Environment.GetEnvironmentVariable(name) ?? fallback)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        /// <summary>JWT issuer claim used at generation AND validation time.</summary>
        public static string JwtIssuer => Or("JWT_ISSUER", DefaultIssuer);

        /// <summary>JWT audience claim used at generation AND validation time.</summary>
        public static string JwtAudience => Or("JWT_AUDIENCE", DefaultAudience);

        /// <summary>
        /// HS256 signing secret. Required and must be ≥ 32 bytes —
        /// otherwise identity tokens can be forged or the signing
        /// library throws a confusing runtime error later.
        /// </summary>
        public static string JwtSecretKey => ValidateSecret(Required("JWT_SECRET_KEY"));

        private static string ValidateSecret(string secret)
        {
            if (Encoding.UTF8.GetByteCount(secret) < 32)
                throw new InvalidOperationException(
                    "JWT_SECRET_KEY is too weak: HS256 needs at least 32 bytes (256 bits). " +
                    "Generate one with: python -c \"import secrets; print(secrets.token_urlsafe(48))\"");

            return secret;
        }
    }
}