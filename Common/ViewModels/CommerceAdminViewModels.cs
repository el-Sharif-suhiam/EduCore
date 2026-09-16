using System;
using System.Collections.Generic;
using System.Text;
using Common.Enums;

namespace Common.ViewModels
{
    /// <summary>Admin-only payment row (joined user). Status serializes as the enum number.</summary>
    public class PaymentAdminViewModel
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal Price { get; set; }
        public short? DiscountId { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? PaymentMethod { get; set; }
        public enPaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
        public decimal FinalPrice { get; set; }
    }

    /// <summary>Admin-only order row (joined user). Status serializes as the enum number.</summary>
    public class OrderAdminViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public decimal TotalPrice { get; set; }
        public enOrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}