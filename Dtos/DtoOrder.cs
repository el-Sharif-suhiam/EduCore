using System;
using System.Collections.Generic;
using System.Text;
using Common;
using Common.Enums;
namespace Dtos
{
    public class DtoOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public decimal? TotalAmount { get; set; }

        public enOrderStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        
    }
}
