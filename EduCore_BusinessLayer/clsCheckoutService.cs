using Common.Enums;
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

            return await clsGeneralData.ExecuteTransaction(
                async (conn, tx) =>
                {
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
