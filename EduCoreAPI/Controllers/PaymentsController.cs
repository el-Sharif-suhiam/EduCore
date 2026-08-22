using Common.Dtos;
using Common.Exceptions;
using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Models.RequestModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace EduCoreAPI.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
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

        // =========================
        // PUT: Mark As Succeeded
        // بعد تأكيد Payment Gateway
        // يسجل TransactionId و PaidAt
        // =========================
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