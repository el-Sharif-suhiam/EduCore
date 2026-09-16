using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace EduCore_BusinessLayer
{
    public class clsPayment
    {
        private DtoPayment _Payment;

        public int Id => _Payment.Id;
        public int OrderId => _Payment.OrderId;
        public string? PaymentMethod => _Payment.PaymentMethod;
        public enPaymentStatus Status => _Payment.Status;
        public string? TransactionId => _Payment.TransactionId;
        public string IdempotencyKey => _Payment.IdempotencyKey;
        public decimal FinalPrice => _Payment.FinalPrice;
        public short? DiscountId => _Payment.DiscountId;

        public bool IsPending => Status == enPaymentStatus.Pending;
        public bool IsSucceeded => Status == enPaymentStatus.Succeeded;
        public bool IsFailed => Status == enPaymentStatus.Failed;
        public bool IsExpired => Status == enPaymentStatus.Expired;

        public clsPayment()
        {
            _Payment = new DtoPayment
            {
                Status = enPaymentStatus.Pending
            };
        }

        private clsPayment(DtoPayment payment)
        {
            _Payment = payment
                ?? throw new ArgumentNullException(nameof(payment));
        }

        public void SetPaymentMethod(string? paymentMethod)
        {
            _Payment.PaymentMethod =
                string.IsNullOrWhiteSpace(paymentMethod)
                    ? null
                    : paymentMethod.Trim();
        }


        public static async Task<clsPayment> FindAsync(int id)
        {
            if (id <= 0)
                throw new ValidationException("Payment id is not valid.");

            var payment =
                await clsPaymentData.GetPaymentByIdAsync(id);

            if (payment is null)
                throw new NotFoundException("Payment not found.");

            return new clsPayment(payment);
        }

        public static async Task<List<Common.ViewModels.PaymentAdminViewModel>> GetAllPaymentsView(int pageNumber, int pageSize, string searchText = "")
            => await clsPaymentData.GetAllPaymentsView(pageNumber, pageSize, searchText);

        private static string GenerateIdempotencyKey()
        {
            return Guid.NewGuid().ToString("N");
        }
        public async Task<bool> CreatePayment(
            int orderId,
            short? discountId = null,
            string? idempotencyKey = null)
        {
            clsOrder order = await clsOrder.Find(orderId);

            if (order.Status != enOrderStatus.Pending)
                throw new ValidationException("Order is not valid for payment");

            if (order.TotalPrice <= 0)
                throw new ValidationException("Order total is invalid");

            string key = string.IsNullOrWhiteSpace(idempotencyKey)
                ? GenerateIdempotencyKey()
                : idempotencyKey.Trim();

            if (key.Length > 255)
                throw new ValidationException("Idempotency key is too long");

            DtoPayment? existing =
                await clsPaymentData.GetPaymentByIdempotencyKeyAsync(key);

            if (existing != null)
            {
                if (existing.OrderId != orderId)
                    throw new ConflictException("Idempotency key already used for another order");

                _Payment = existing;
                return true;
            }

            DtoPaymentInitRespone? response;

            try
            {
                response =
                    await clsPaymentData.CreatePaymentAsync(
                        orderId,
                        key,
                        _Payment.PaymentMethod,
                        discountId);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                existing = await clsPaymentData.GetPaymentByIdempotencyKeyAsync(key);

                if (existing != null && existing.OrderId == orderId)
                {
                    _Payment = existing;
                    return true;
                }

                throw;
            }

            if (response is null)
                throw new ConflictException("Error creating payment");

            _Payment.Id = response.Id;
            _Payment.IdempotencyKey = key;
            _Payment.FinalPrice = response.FinalPrice;

            await clsAudit.LogAsync(
                userId: order.UserId,
                actionType: enAuditActionType.CreatePayment,
                entityType: "Payment",
                entityId: response.Id,
                description: $"Payment created for order {orderId}");

            return response.Id > 0;
        }

        public async Task<bool> MarkAsSucceededAsync(
            int userId,
            string transactionId,
            SqlConnection conn,
            SqlTransaction tx,
            DateTime? paidAt = null)
        {
            if (_Payment.Id <= 0)
                throw new ValidationException("Invalid payment id.");

            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ValidationException("Transaction id is required.");

            if (_Payment.Status == enPaymentStatus.Succeeded)
                return true;

            if (_Payment.Status != enPaymentStatus.Pending)
                throw new ConflictException("Payment already processed");

            string cleanTransactionId = transactionId.Trim();
            DateTime finalPaidAt = paidAt ?? DateTime.UtcNow;

            bool updated =
                await clsPaymentData.UpdateStatusWithTransaction(
                    _Payment.Id,
                    enPaymentStatus.Succeeded,
                    cleanTransactionId,
                    finalPaidAt,
                    conn,
                    tx);

            if (!updated)
                throw new ConflictException("Failed to update payment status");

            _Payment.Status = enPaymentStatus.Succeeded;
            _Payment.TransactionId = cleanTransactionId;
            _Payment.PaidAt = finalPaidAt;

            await clsAudit.LogAsync(
                userId,
                enAuditActionType.PaymentSucceeded,
                "Payment",
                _Payment.Id,
                $"Payment succeeded. TransactionId: {cleanTransactionId}",
                null,null,
                conn,
                tx);

            return updated;
        }


        public async Task<bool> MarkAsFailedAsync(
            int userId,
            string? transactionId = null)
        {
            if (_Payment.Id <= 0)
                throw new ValidationException("Invalid payment id.");

            if (_Payment.Status == enPaymentStatus.Failed)
                return true;

            bool updated =
                await clsPaymentData.UpdateStatus(
                    _Payment.Id,
                    enPaymentStatus.Failed,
                    transactionId,
                    null);

            if (!updated)
                throw new ConflictException("Failed to update payment status");

            _Payment.Status = enPaymentStatus.Failed;

            await clsAudit.LogAsync(
                userId,
                enAuditActionType.PaymentFailed,
                "Payment",
                _Payment.Id,
                "Payment failed");

            return updated;
        }


        public async Task<bool> MarkAsExpiredAsync(
            int userId)
        {
            if (_Payment.Id <= 0)
                throw new ValidationException("Invalid payment id.");

            if (_Payment.Status == enPaymentStatus.Expired)
                return true;

            bool updated =
                await clsPaymentData.UpdateStatus(
                    _Payment.Id,
                    enPaymentStatus.Expired,
                    null,
                    null);

            if (!updated)
                throw new ConflictException("Failed to update payment status");

            _Payment.Status = enPaymentStatus.Expired;

            await clsAudit.LogAsync(
                userId,
                enAuditActionType.PaymentExpired,
                "Payment",
                _Payment.Id,
                "Payment expired");

            return updated;
        }
    }
}