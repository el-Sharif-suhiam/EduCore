using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace EduCore_BusinessLayer
{
    // ============================================================
    // ============================================================
    // ======                                        ==============
    // ======   PAYMENT GATEWAY: STRIPE              ==============
    // ======   >>> THIS IS THE FILE TO CHANGE <<<   ==============
    // ======   when switching or adding a gateway.  ==============
    // ======                                        ==============
    // ============================================================
    // ============================================================
    //
    // Talks to Stripe's REST API directly with HttpClient —
    // deliberately NO official Stripe NuGet SDK (owner decision:
    // minimum tech). Everything a provider needs lives here:
    //
    //   1. CreateCheckoutSessionAsync  → hosted-checkout URL
    //   2. VerifyWebhookSignature      → HMAC-SHA256 verification
    //
    // Required environment variables (see EduCoreAPI/.env.example):
    //   STRIPE_SECRET_KEY       sk_test_… / sk_live_…
    //   STRIPE_WEBHOOK_SECRET   whsec_… (for VerifyWebhookSignature)
    //   FRONTEND_URL            e.g. http://localhost:3000
    //
    // Callers:
    //   - PaymentsController.CreateStripeCheckoutSession (step 3)
    //   - StripeWebhookController (source of truth, step 6)
    //
    // To SWITCH PROVIDER: rewrite this class + the webhook
    // controller only. Nothing else in the codebase knows about
    // Stripe.
    public static class clsStripeGateway
    {
        // ======================================================
        // ====== STRIPE: CHANGE HERE (API base / currency) =====
        // ======================================================
        private const string ApiBase = "https://api.stripe.com/v1";

        /// <summary>Three-letter ISO currency code sent to Stripe.</summary>
        private const string Currency = "usd";

        /// <summary>Max age of a webhook timestamp before rejection.</summary>
        private const int WebhookToleranceSeconds = 300;

        private static readonly HttpClient _http = CreateHttpClient();

        public sealed record CheckoutSession(string SessionId, string Url);

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(ApiBase) };
            var secret = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", secret);
            }
            return client;
        }

        /// <summary>True when STRIPE_SECRET_KEY is present.</summary>
        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY"));

        // ------------------------------------------------------------
        // STEP 3 — create the hosted Checkout Session.
        // Returns the session id + the URL the browser must be
        // redirected to. Retries are safe: our payment's unique
        // IdempotencyKey scope makes Stripe return the SAME session.
        // ------------------------------------------------------------
        public static async Task<CheckoutSession> CreateCheckoutSessionAsync(
            int paymentId,
            decimal finalPrice,
            string itemName,
            string successUrl,
            string cancelUrl,
            string? idempotencyKey = null)
        {
            if (!IsConfigured)
                throw new ValidationException(
                    "Payment gateway is not configured on the server (STRIPE_SECRET_KEY missing).");

            long unitAmount = (long)Math.Round(finalPrice * 100m, MidpointRounding.AwayFromZero);
            if (unitAmount <= 0)
                throw new ValidationException("Payment amount must be greater than zero.");

            var form = new Dictionary<string, string>
            {
                ["mode"] = "payment",
                ["success_url"] = successUrl,
                ["cancel_url"] = cancelUrl,

                // Our identifiers travel with the session so the webhook
                // can find the payment WITHOUT trusting the browser.
                ["client_reference_id"] = paymentId.ToString(),
                ["metadata[payment_id]"] = paymentId.ToString(),

                ["line_items[0][quantity]"] = "1",
                ["line_items[0][price_data][currency]"] = Currency,
                ["line_items[0][price_data][unit_amount]"] = unitAmount.ToStringInvariant(),
                ["line_items[0][price_data][product_data][name]"] = Truncate(itemName, 120),
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "checkout/sessions")
            {
                Content = new FormUrlEncodedContent(form),
            };
            // Prefer the payment's own idempotency key (unique index in
            // Payments table); fall back to a deterministic derived key.
            var key = string.IsNullOrWhiteSpace(idempotencyKey)
                ? $"educore-payment-{paymentId}"
                : $"educore-payment-{paymentId}-{idempotencyKey}";
            request.Headers.TryAddWithoutValidation("Idempotency-Key", key);

            using var response = await _http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string message = ExtractStripeErrorMessage(body)
                    ?? $"HTTP {(int)response.StatusCode}";
                throw new InvalidOperationException(
                    $"Stripe rejected checkout session creation: {message}");
            }

            using var doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            string sessionId = GetStringOrNull(root, "id")
                ?? throw new InvalidOperationException("Stripe response missing session id.");
            string url = GetStringOrNull(root, "url")
                ?? throw new InvalidOperationException("Stripe response missing checkout url.");

            return new CheckoutSession(sessionId, url);
        }

        // ------------------------------------------------------------
        // WEBHOOK SIGNATURE VERIFICATION (no SDK).
        // Stripe-Signature header format: t=<unix_ts>,v1=<hex>,v1=<hex>…
        // Signed payload = "<t>.<raw_body>"; signature = HMAC-SHA256
        // of that with STRIPE_WEBHOOK_SECRET. Any v1 match passes;
        // comparisons are constant-time; old timestamps are rejected.
        // ------------------------------------------------------------
        public static bool VerifyWebhookSignature(
            string payload,
            string signatureHeader,
            string webhookSecret,
            int? toleranceSeconds = null)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(webhookSecret))
                return false;

            string? timestamp = null;
            var signatures = new List<string>();

            foreach (var part in signatureHeader.Split(',', StringSplitOptions.TrimEntries))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string name = part[..eq];
                string value = part[(eq + 1)..];
                if (name == "t") timestamp = value;
                else if (name == "v1") signatures.Add(value);
            }

            if (timestamp is null || signatures.Count == 0)
                return false;

            if (!long.TryParse(timestamp, out long unixTime))
                return false;

            long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unixTime;
            int tolerance = toleranceSeconds ?? WebhookToleranceSeconds;
            if (Math.Abs(age) > tolerance)
                return false;

            string signedPayload = $"{timestamp}.{payload}";
            byte[] expected = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(webhookSecret),
                Encoding.UTF8.GetBytes(signedPayload));

            foreach (string candidate in signatures)
            {
                if (TryHexToBytes(candidate, out byte[] actual) &&
                    CryptographicOperations.FixedTimeEquals(expected, actual))
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------- helpers ----------------

        private static string? ExtractStripeErrorMessage(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("error", out JsonElement error))
                {
                    string? msg = GetStringOrNull(error, "message");
                    return msg;
                }
            }
            catch (JsonException)
            {
                // fall through
            }
            return null;
        }

        private static string? GetStringOrNull(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement prop) &&
                   prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        private static bool TryHexToBytes(string hex, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (hex.Length % 2 != 0) return false;
            var result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                if (!byte.TryParse(hex.AsSpan(i * 2, 2),
                        System.Globalization.NumberStyles.HexNumber,
                        null, out result[i]))
                {
                    return false;
                }
            }
            bytes = result;
            return true;
        }

        private static string ToStringInvariant(this long value) =>
            value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
