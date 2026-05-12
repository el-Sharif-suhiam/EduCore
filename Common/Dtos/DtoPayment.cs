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

        public DateTime PaidAt { get; set; }

        public decimal Price { get; set; }

        public short? DiscountId { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string PaymentMethod { get; set; }

        public enPaymentStatus Status { get; set; }

        public string TransactionId { get; set; }

        public decimal PayedPrice { get; set; }
    }
}
