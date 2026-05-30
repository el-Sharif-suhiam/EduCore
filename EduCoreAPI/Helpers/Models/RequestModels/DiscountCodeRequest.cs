namespace EduCoreAPI.Helpers.Models.RequestModels
{
    public record DiscountCodeRequest
    (string DiscountCode,
    decimal? DiscountRate,
    int CreatedById,
    DateTime? ExpireAt,
    short? AllowedUseNumber,  // عدد الاستخدام لكل مستخدم (اختياري)
    short? TotalUsedNumber  // الحد الأقصى لعدد المستخدمين (اختياري)
    );
    
}
