using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Enums
{
    public enum enAuditActionType
    {
        Login,
        Logout,

        CreateLesson,
        DeleteLesson,
        UpdateLesson,
        UnDeleteLesson,

        CreateCourse,
        DeleteCourse,
        UpdateCourse,
        UnDeleteCourse,
        CreateBundle,
        DeleteBundle,
        UpdateBundle,

        CreateOrder,
        UpdateOrder,

        CreateDiscount,
        UpdateDiscount,
        DeleteDiscount,
        CreatePayment,
        UpdatePayment,
        PaymentSucceeded,
        PaymentFailed,
        PaymentExpired,
        UserPromotedToAdmin,
        UserPromotedToInstructor,
        UserRemovedFromAdmins,
        UserRemovedFromInstructors,

        EnrolledToProduct,

        CreateUser,
        UpdateUser,
        DeleteUser,

        UserReactivated
    }
}
