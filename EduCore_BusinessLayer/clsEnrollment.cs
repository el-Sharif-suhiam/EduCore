using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using EduCore_DataAccess;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EduCore_BusinessLayer
{
    public class clsEnrollment
    {

        public static async Task<bool> CreateFromPaymentAsync(int userId,
        clsPayment payment,
        SqlConnection conn,
        SqlTransaction tx)
        {
            if (payment == null)
                throw new ValidationException("Payment is null.");

            if (payment.Status != enPaymentStatus.Succeeded)
                throw new ConflictException(
                    "Payment is not succeeded.");

            var enrollment = new DtoEnrollmentDataRequest
            {
                PaymentId = payment.Id,

                orderId = payment.OrderId,

                ExpireAt = DateTime.UtcNow.AddMonths(
                    clsGeneralRules.DefaultExpireDateByMonths)
            };

            bool result =
                await clsEnrollmentsData.AddEnrollment(
                    enrollment,
                    conn,
                    tx);

            if (!result)
                throw new ConflictException(
                    "Failed to create enrollments.");

            await clsAudit.LogAsync(
              userId,
              enAuditActionType.EnrolledToProduct,
              "Enrollment",
              payment.Id,
              $"User {userId} enrolled via payment {payment.Id}",null,null,
              conn,
              tx);

            return true;
        }

        public static async Task<bool> IsUserEnrolled(int userId, int productId)
        {
            return await clsEnrollmentsData.IsUserEnrolled(userId, productId);
        }

    }
}
