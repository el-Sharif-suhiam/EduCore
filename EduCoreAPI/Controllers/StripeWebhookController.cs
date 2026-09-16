using System.Text.Json;
using EduCore_BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCoreAPI.Controllers
{
    // ============================================================
    // ============================================================
    // ======                                        ==============
    // ======   PAYMENT GATEWAY WEBHOOK: STRIPE      ==============
    // ======   SOURCE OF TRUTH FOR PAID = SUCCEEDED ==============
    // ======                                        ==============
    // ============================================================
    // ============================================================
    //
    // Stripe calls this endpoint when money actually moves:
    //   POST api/webhooks/stripe
    //
    // SECURITY MODEL (fixes audit finding H1):
    //   * [AllowAnonymous] because Stripe cannot hold a user JWT.
    //   * Every request is authenticated by the HMAC-SHA256
    //     Stripe-Signature header (verified in clsStripeGateway).
    //   * The payment id is taken from the session's
    //     client_reference_id / metadata — set SERVER-side at
    //     session creation, never from the browser.
    //   * Completion runs the existing transactional chain:
    //       clsCheckoutService.CompletePaymentAsync
    //       (payment → discount usage → enrollments → order → cleanup)
    //   * Replays/no-ops are safe: MarkAsSucceededAsync ignores
    //     already-succeeded payments and rejects non-pending ones.
    //
    // Local testing (Stripe CLI):
    //   stripe listen --forward-to localhost:5087/api/webhooks/stripe
    //   → copy the printed whsec_… into STRIPE_WEBHOOK_SECRET in .env
    //   stripe trigger checkout.session.completed
    //
    // To SWITCH PROVIDER: change clsStripeGateway + THIS controller.
    [ApiController]
    [Route("api/webhooks/stripe")]
    public class StripeWebhookController : ControllerBase
    {
        /// <summary>Event types this endpoint knows how to apply.</summary>
        private const string SessionCompleted = "checkout.session.completed";
        private const string SessionAsyncSucceeded = "checkout.session.async_payment_succeeded";
        private const string SessionExpired = "checkout.session.expired";
        private const string SessionAsyncFailed = "checkout.session.async_payment_failed";

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Handle()
        {
            string? secret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
            if (string.IsNullOrWhiteSpace(secret))
            {
                // 503 tells Stripe to retry later once ops fix config.
                return Problem(
                    title: "Webhook not configured",
                    detail: "STRIPE_WEBHOOK_SECRET is missing on the server.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            // Raw body is REQUIRED for signature verification — never
            // deserialize before verifying.
            string payload;
            using (var reader = new StreamReader(Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            string signatureHeader = Request.Headers["Stripe-Signature"].ToString();

            if (!clsStripeGateway.VerifyWebhookSignature(payload, signatureHeader, secret))
            {
                return Problem(
                    title: "Invalid webhook signature",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            string? eventType = GetString(root, "type");
            JsonElement dataObject =
                root.TryGetProperty("data", out JsonElement data) &&
                data.TryGetProperty("object", out JsonElement obj)
                    ? obj
                    : default;

            switch (eventType)
            {
                case SessionCompleted:
                case SessionAsyncSucceeded:
                    await ApplySessionCompleted(dataObject);
                    break;

                case SessionExpired:
                    await ApplySessionTerminalState(dataObject, markAsFailed: false);
                    break;

                case SessionAsyncFailed:
                    await ApplySessionTerminalState(dataObject, markAsFailed: true);
                    break;

                default:
                    // Unknown events are acknowledged so Stripe stops retrying.
                    break;
            }

            return Ok(new { received = true });
        }

        // ------------------------------------------------------------
        // Money received → complete the whole purchase atomically.
        // ------------------------------------------------------------
        private async Task ApplySessionCompleted(JsonElement session)
        {
            int? paymentId = ExtractPaymentId(session);
            if (paymentId is null)
                return; // Not ours / unmappable — acknowledge.

            var payment = await clsPayment.FindAsync(paymentId.Value); // throws NotFound

            if (!payment.IsPending)
                return; // Already processed (webhook replay or manual op) — no-op.

            var order = await clsOrder.Find(payment.OrderId);

            // Prefer the PaymentIntent id as the durable gateway reference.
            string transactionId =
                GetString(session, "payment_intent")
                ?? GetString(session, "id")
                ?? $"stripe-session-{paymentId}";

            await clsCheckoutService.CompletePaymentAsync(
                order.UserId,
                payment,
                transactionId);
        }

        // ------------------------------------------------------------
        // Session expired / async payment failed → close the payment.
        // ------------------------------------------------------------
        private async Task ApplySessionTerminalState(JsonElement session, bool markAsFailed)
        {
            int? paymentId = ExtractPaymentId(session);
            if (paymentId is null)
                return;

            var payment = await clsPayment.FindAsync(paymentId.Value);

            if (!payment.IsPending)
                return;

            var order = await clsOrder.Find(payment.OrderId);

            if (markAsFailed)
            {
                await payment.MarkAsFailedAsync(order.UserId, GetString(session, "id"));
            }
            else
            {
                await payment.MarkAsExpiredAsync(order.UserId);
            }
        }

        // ------------------------------------------------------------
        // payment id travels server-side with the session:
        // client_reference_id first, metadata.payment_id fallback.
        // ------------------------------------------------------------
        private static int? ExtractPaymentId(JsonElement session)
        {
            string? raw =
                GetString(session, "client_reference_id") ??
                (session.TryGetProperty("metadata", out JsonElement metadata)
                    ? GetString(metadata, "payment_id")
                    : null);

            return int.TryParse(raw, out int id) && id > 0 ? id : null;
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object &&
                   element.TryGetProperty(propertyName, out JsonElement prop) &&
                   prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }
    }
}
