using System;
using System.Security.Cryptography;
using System.Text;
using EduCore_BusinessLayer;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Unit tests for the raw Stripe webhook HMAC verification
    /// (no DB, no network): signature math, tamper detection, replay
    /// (timestamp) tolerance, and malformed headers.
    /// </summary>
    public class StripeWebhookSignatureTests
    {
        private const string Secret = "whsec_test_secret_0123456789abcdef";

        private static (string payload, string header) Signed(string payload, string secret, long ageSeconds = 0)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ageSeconds;
            string signed = $"{timestamp}.{payload}";
            byte[] hash = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes(signed));
            return (payload, $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}");
        }

        [Fact]
        public void VerifyWebhookSignature_ValidSignature_Passes()
        {
            (string payload, string header) = Signed(
                """{"type":"checkout.session.completed","data":{"object":{"id":"cs_1"}}}""", Secret);

            Assert.True(clsStripeGateway.VerifyWebhookSignature(payload, header, Secret));
        }

        [Fact]
        public void VerifyWebhookSignature_TamperedPayload_Fails()
        {
            (string payload, string header) = Signed(
                """{"type":"checkout.session.completed","data":{"object":{"id":"cs_original"}}}""", Secret);

            string tampered = payload.Replace("cs_original", "cs_999999");

            Assert.False(clsStripeGateway.VerifyWebhookSignature(tampered, header, Secret));
        }

        [Fact]
        public void VerifyWebhookSignature_WrongSecret_Fails()
        {
            (string payload, string header) = Signed(
                """{"type":"checkout.session.completed"}""", Secret);

            Assert.False(clsStripeGateway.VerifyWebhookSignature(payload, header, "whsec_attacker_owned"));
        }

        [Fact]
        public void VerifyWebhookSignature_OldTimestamp_Fails()
        {
            (string payload, string header) = Signed(
                """{"type":"checkout.session.completed"}""", Secret, ageSeconds: 3600);

            Assert.False(clsStripeGateway.VerifyWebhookSignature(payload, header, Secret));
        }

        [Fact]
        public void VerifyWebhookSignature_ReplayOfOldSignedPayload_Fails()
        {
            // Captured traffic older than the tolerance window must be
            // rejected even with a valid signature (replay protection).
            (string payload, string header) = Signed(
                """{"type":"checkout.session.completed"}""", Secret, ageSeconds: 301);

            Assert.False(clsStripeGateway.VerifyWebhookSignature(payload, header, Secret));
        }

        [Fact]
        public void VerifyWebhookSignature_MalformedHeader_Fails()
        {
            string payload = """{"type":"checkout.session.completed"}""";

            Assert.False(clsStripeGateway.VerifyWebhookSignature(payload, "", Secret));
            Assert.False(clsStripeGateway.VerifyWebhookSignature(payload, "v1=deadbeef", Secret));
            Assert.False(clsStripeGateway.VerifyWebhookSignature(payload, "not-a-signature", Secret));
        }
    }
}
