using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoEnrollmentDataRequest
    {
        public int orderId {  get; set; }
        public int PaymentId { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
