namespace EduCoreAPI.Helpers.Models.ResponeModels
{
    public class DiscountCodeResponse
    {
        public int Id { get; set; }
        public string DiscountCode { get; set; }
        public decimal? DiscountRate { get; set; }

        public DateTime? ExpireAt { get; set; }

        public short? AllowedUseNumber { get; set; }   // عدد الاستخدام لكل مستخدم (اختياري)
        public short? TotalUsedNumber { get; set; }    // الحد الأقصى لعدد المستخدمين (اختياري)

        public bool IsUnlimited { get; set; }
        public bool IsExpired { get; set; }
        public bool IsValid { get; set; }

    }
}
