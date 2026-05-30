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
                    // 2) Create enrollments
                    // =========================

                    await clsEnrollment.CreateFromPaymentAsync(userId,
                        payment,
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
