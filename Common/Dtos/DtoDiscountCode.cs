using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoDiscountCode
    {
        public short Id { get; set; }

        public string DiscountCode { get; set; }
        public decimal? DiscountRate { get; set; }

        public int CreatedById { get; set; }

        public DateTime? ExpireAt { get; set; }

        public short? AllowedUseNumber { get; set; }   // عدد الاستخدام لكل مستخدم (اختياري)
        public short? TotalUsedNumber { get; set; }    // الحد الأقصى لعدد المستخدمين (اختياري)


    }
}
