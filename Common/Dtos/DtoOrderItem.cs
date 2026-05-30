using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoOrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int ProductId { get; set; }

        public decimal? PriceAtPurchase { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string ProductType { get; set; }

    }

    public class DtoOrderItemRespone
    {
        public int Id { get; set; }
        public decimal NewOrderTotal { get; set; }
    }
}
