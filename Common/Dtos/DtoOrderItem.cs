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

        public bool MarkedToDelete { get; set; } 
    }
}
