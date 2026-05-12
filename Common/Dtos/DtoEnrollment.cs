using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoEnrollment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? ExpireAt { get; set; }
        public int PaymentId { get; set; }
    }
}
