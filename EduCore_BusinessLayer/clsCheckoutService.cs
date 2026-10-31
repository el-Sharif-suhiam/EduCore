using Common.Enums;
using Common.Exceptions;
using EduCore_DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EduCore_BusinessLayer
{
    public class clsCheckoutService
    {

        public static async Task<bool> CompletePaymentAsync(int userId,
        clsPayment payment,
        string transactionId)
        {
            if (payment == null)
                throw new ValidationException("Payment is null.");

            // Idempotency gate: only a Pending payment may drive the
            // completion chain. Replays (already Succeeded) and terminal
            // states (Failed/Expired) must never re-apply side effects
            // (discount usage, enrollments, order completion).
            if (payment.Status != enPaymentStatus.Pending)
                throw new ConflictException("Payment already processed");

            return await clsGeneralData.ExecuteTransaction(
                async (conn, tx) =>
                {
                    // =========================
                    // 0) Lock the order row FIRST to keep a consistent
                    //    order-first lock-acquisition order with the
                    //    order-mutation stored procedures.
                    // =========================
                    await clsOrderData.LockOrderAsync(payment.OrderId, conn, tx);

                    // =========================
                    // 0b) Lock the discount row (if any) BEFORE the payment
                    //     status transition. Establishes a consistent
                    //     Order -> Discount -> Payment lock order with
                    //     SP_CreateNewPayment, preventing lock-order inversion
                    //     between Payments and DiscountCodes.
                    // =========================
                    if (payment.DiscountId.HasValue)
                    {
                        await clsDiscountCodesData.LockDiscountAsync(
                            payment.DiscountId.Value, conn, tx);
                    }

                    // =========================
                    // 1) Mark payment succeeded
                    // =========================

                    bool paymentUpdated =
                        await payment.MarkAsSucceededAsync(userId,
                            transactionId,
                            conn,
                            tx);

                    if (!paymentUpdated)
                        return false;

                    // =========================
                    // 2) Increment discount usage
                    // =========================

                    if (payment.DiscountId.HasValue)
                    {
                        await clsDiscountCodesData.IncrementUsage(
                            payment.DiscountId.Value,
                            conn,
                            tx);
                    }

                    // =========================
                    // 3) Create enrollments
                    // =========================

                    await clsEnrollment.CreateFromPaymentAsync(userId,
                        payment,
                        conn,
                        tx);

                    // =========================
                    // 4) Complete the order
                    // =========================

                    bool orderCompleted =
                        await clsOrderData.UpdateStatus(
                            payment.OrderId,
                            enOrderStatus.Completed.ToString(),
                            conn,
                            tx);

                    if (!orderCompleted)
                        return false;

                    // =========================
                    // 5) Expire any other pending payments
                    // =========================

                    await clsPaymentData.ExpirePendingPaymentsForOrder(
                        payment.OrderId,
                        conn,
                        tx);

                    // =========================
                    // later:
                    // invoice
                    // notifications
                    // analytics
                    // =========================

                    return true;
                });
        }
    }
}
