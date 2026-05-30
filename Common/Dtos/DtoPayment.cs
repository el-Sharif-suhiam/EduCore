using Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoPayment
    {

        public int Id { get; set; }

        public int OrderId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public decimal Price { get; set; }

        public short? DiscountId { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string? PaymentMethod { get; set; }

        public enPaymentStatus Status { get; set; }

        public string? TransactionId { get; set; }
        public string IdempotencyKey { get; set; }
        public decimal FinalPrice { get; set; }
    }

    public class DtoPaymentInitRespone
    {
        public int Id { get; set; }
        public decimal FinalPrice { get; set; }
        public string IdempotencyKey { get; set; }
    }
}
