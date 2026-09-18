using EduCoreAPI.Helpers;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Guards the production config seams added in the production
    /// hardening pass:
    ///   • JWT issuer/audience/secret share ONE source (EnvConfig)
    ///   • the API fails FAST at startup on a missing/weak secret
    ///   • the CORS allow-list is parsed safely from env input
    ///
    /// These tests mutate process environment variables, so they are
    /// kept in a single class (xunit runs a class's tests sequentially)
    /// and every mutation restores the previous value afterwards.
    /// </summary>
    public class EnvConfigTests
    {
        private const string SecretKey = "JWT_SECRET_KEY";
        private const string IssuerKey = "JWT_ISSUER";
        private const string AudienceKey = "JWT_AUDIENCE";
        private const string CorsKey = "CORS_ALLOWED_ORIGINS";

        [Fact]
        public void JwtIssuer_Defaults_To_Expected_Value()
            => WithEnv(IssuerKey, null, () => Assert.Equal("EduCoreApi", EnvConfig.JwtIssuer));

        [Fact]
        public void JwtAudience_Defaults_To_Expected_Value()
            => WithEnv(AudienceKey, null, () => Assert.Equal("EduCoreApiUsers", EnvConfig.JwtAudience));

        [Fact]
        public void JwtIssuer_Respects_Override()
            => WithEnv(IssuerKey, "https://issuer.example",
                () => Assert.Equal("https://issuer.example", EnvConfig.JwtIssuer));

        [Fact]
        public void Required_Throws_When_Unset()
            => WithEnv(SecretKey, null, () =>
            {
                var ex = Assert.Throws<InvalidOperationException>(() => EnvConfig.JwtSecretKey);
                Assert.Contains("JWT_SECRET_KEY", ex.Message);
            });

        [Fact]
        public void JwtSecretKey_Throws_When_Too_Short()
            => WithEnv(SecretKey, new string('a', 20), () =>
            {
                var ex = Assert.Throws<InvalidOperationException>(() => EnvConfig.JwtSecretKey);
                Assert.Contains("32", ex.Message);
            });

        [Fact]
        public void JwtSecretKey_Accepts_Strong_Secret()
            => WithEnv(SecretKey, new string('z', 48),
                () => Assert.Equal(new string('z', 48), EnvConfig.JwtSecretKey));

        [Fact]
        public void List_Splits_Trims_And_Drops_Empties()
            => WithEnv(CorsKey, "  https://app.example ,  , http://admin.example",
                () => Assert.Equal(
                    new[] { "https://app.example", "http://admin.example" },
                    EnvConfig.List(CorsKey, "http://localhost:5087")));

        [Fact]
        public void List_Falls_Back_When_Unset()
            => WithEnv(CorsKey, null,
                () => Assert.Equal(
                    new[] { "http://localhost:5087" },
                    EnvConfig.List(CorsKey, "http://localhost:5087")));

        private static void WithEnv(string name, string? value, Action body)
        {
            string? previous = Environment.GetEnvironmentVariable(name);
            try
            {
                Environment.SetEnvironmentVariable(name, value);
                body();
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, previous);
            }
        }
    }
}