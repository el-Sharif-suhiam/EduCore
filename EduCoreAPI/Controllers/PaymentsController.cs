using Common.Dtos;
using Common.Exceptions;
using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Models.RequestModels;
using EduCoreAPI.Helpers.Dtos.RequestDto;
using EduCoreAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;namespace EduCoreAPI.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        // =========================
        // GET: All payments (admin feed)
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<ActionResult<List<Common.ViewModels.PaymentAdminViewModel>>> GetAllPayments(
            [FromQuery] PageRequest pageRequest, string? search)
        {
            clsApiValidators.ValidatePaging(pageRequest);

            var payments = await clsPayment.GetAllPaymentsView(
                pageRequest.PageNumber,
                pageRequest.PageSize,
                search ?? "");

            return Ok(payments);
        }

        // =========================
        // GET: Payment By Id
        // =========================
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetPaymentById([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsPayment payment = await clsPayment.FindAsync(id);

            clsOrder order = await clsOrder.Find(payment.OrderId);

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               order.UserId,
               "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            return Ok(new
            {
                payment.Id,
                payment.OrderId,
                payment.PaymentMethod,
                payment.Status,
                payment.TransactionId,
                payment.IdempotencyKey,
                payment.FinalPrice,
                payment.IsPending,
                payment.IsSucceeded,
                payment.IsFailed,
                payment.IsExpired
            });
        }

        // =========================
        // POST: Create Payment
        // يرجع Id + FinalPrice + IdempotencyKey
        // بناءً على DtoPaymentInitRespone
        // =========================
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> CreatePayment([FromBody] CreatePaymentRequest request, [FromServices] IAuthorizationService authorizationService)
        {
            clsOrder order = await clsOrder.Find(request.OrderId);

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               order.UserId,
               "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            clsPayment payment = new clsPayment();

            short? discountId = null;

            if (!string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                clsDiscountCode discount =
                    await clsDiscountCode.Find(request.DiscountCode);

                if (!discount.IsValid)
                    throw new ValidationException("Discount code is not valid.");

                discountId = discount.Id;
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                payment.SetPaymentMethod(request.PaymentMethod);
            }

            bool result = await payment.CreatePayment(
                request.OrderId,
                discountId,
                request.IdempotencyKey);

            if (!result)
                throw new ConflictException("Failed to create payment.");

            return Ok(new DtoPaymentInitRespone
            {
                Id = payment.Id,
                FinalPrice = payment.FinalPrice,
                IdempotencyKey = payment.IdempotencyKey
            });
        }

        // ============================================================
        // ============ PAYMENT GATEWAY: STRIPE =======================
        // ============================================================
        // POST: Create a Stripe hosted-checkout session for a payment.
        //
        //   POST api/payments/{id}/stripe-checkout-session
        //     -> { url, sessionId }
        //
        // The CLIENT then redirects to `url` (see
        // frontend/src/lib/payments.ts). The payment is NOT marked
        // succeeded here — the webhook (StripeWebhookController) is
        // the source of truth. This fixes audit finding H1: users can
        // no longer self-declare success via checkOut-succeed.
        //
        // To switch gateway: change clsStripeGateway + webhook only.
        // ============================================================
        [Authorize]
        [HttpPost("{id:int}/stripe-checkout-session")]
        public async Task<ActionResult> CreateStripeCheckoutSession(
            [FromRoute] int id,
            [FromServices] IAuthorizationService authorizationService)
        {
            clsPayment payment = await clsPayment.FindAsync(id);

            clsOrder order = await clsOrder.Find(payment.OrderId);

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               order.UserId,
               "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            if (!payment.IsPending)
                throw new ConflictException("This payment can no longer be processed.");

            // Human-readable item line for the Stripe product name.
            string itemName = order.Items is { Count: > 0 }
                ? string.Join(", ", order.Items.Select(i => i.Name))
                : $"EduCore purchase #{payment.OrderId}";

            // Success/cancel land back on the frontend cart page; it
            // reads ?checkout=success|cancelled&paymentId=… and polls
            // GET /api/payments/{id} until the webhook has landed.
            string frontendUrl =
                Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";

            var session = await clsStripeGateway.CreateCheckoutSessionAsync(
                payment.Id,
                payment.FinalPrice,
                itemName,
                successUrl: $"{frontendUrl}/cart?checkout=success&paymentId={payment.Id}",
                cancelUrl: $"{frontendUrl}/cart?checkout=cancelled&paymentId={payment.Id}",
                idempotencyKey: payment.IdempotencyKey);

            return Ok(new { url = session.Url, sessionId = session.SessionId });
        }

        // ============================================================
        // PUT: Mark As Succeeded  (DEPRECATED — DO NOT USE IN NEW UI)
        // ============================================================
        // Self-service success endpoint kept only for backward
        // compatibility / manual ops. Security audit finding H1:
        // ownership does NOT prove money moved. Real completion must
        // come from the verified Stripe webhook. Remove once nothing
        // calls it.
        // ============================================================
        [Authorize]
        [HttpPut("{id:int}/checkOut-succeed")]
        public async Task<ActionResult> CheckOutSucceeded(
            [FromRoute] int id,
            [FromBody] string TransactionId,
            [FromServices] IAuthorizationService authorizationService)
        {
            clsPayment payment = await clsPayment.FindAsync(id);

            clsOrder order = await clsOrder.Find(payment.OrderId);

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               order.UserId,
               "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);

            bool result = await clsCheckoutService.CompletePaymentAsync(actionbyId, payment, TransactionId);

            if (!result)
                throw new ConflictException("Failed to mark payment as succeeded");

            return Ok(new
            {
                payment.Id,
                payment.TransactionId,
                payment.FinalPrice
            });
        }

        // =========================
        // PUT: Mark As Failed
        // فشل الدفع — TransactionId اختياري
        // =========================
        [Authorize]
        [HttpPut("{id:int}/fail")]
        public async Task<ActionResult> MarkAsFailed(
            [FromRoute] int id,
            [FromBody] string TransactionId,
            [FromServices] IAuthorizationService authorizationService)
        {
            clsPayment payment = await clsPayment.FindAsync(id);

            clsOrder order = await clsOrder.Find(payment.OrderId);

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               order.UserId,
               "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);

            bool result = await payment.MarkAsFailedAsync(actionbyId,TransactionId);

            if (!result)
                throw new ConflictException("Failed to mark payment as failed");

            return Ok(new
            {
                payment.Id,
                payment.OrderId,
                payment.Status
            });
        }

        // =========================
        // PUT: Mark As Expired
        // انتهت صلاحية الدفع
        // لا يحتاج TransactionId
        // =========================
        [Authorize]
        [HttpPut("{id:int}/expire")]
        public async Task<ActionResult> MarkAsExpired(
            [FromRoute] int id,
            [FromServices] IAuthorizationService authorizationService)
        {
            clsPayment payment = await clsPayment.FindAsync(id);

            clsOrder order = await clsOrder.Find(payment.OrderId);

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               order.UserId,
               "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);

            bool result = await payment.MarkAsExpiredAsync(actionbyId);

            if (!result)
                throw new ConflictException("Failed to mark payment as expired");

            return Ok(new
            {
                payment.Id,
                payment.OrderId,
                payment.Status
            });
        }
    }
}