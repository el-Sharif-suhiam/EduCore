using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Models.ResponeModels;

namespace EduCoreAPI.Helpers.Mappers
{
    public class DiscountCodeMapper
    {
        public static DiscountCodeResponse ToDiscountCodeResponse(clsDiscountCode discountCode)
        {
            return new DiscountCodeResponse
            {
                Id = discountCode.Id,
                DiscountCode = discountCode.Code,
                DiscountRate = discountCode.DiscountRate,
                AllowedUseNumber = discountCode.AllowedUseNumber,
                ExpireAt = discountCode.ExpireAt,
                IsExpired = discountCode.IsExpired,
                IsUnlimited = discountCode.IsUnlimited,
                IsValid = discountCode.IsValid,
                TotalUsedNumber = discountCode.TotalUsedNumber
            };
        }
    }
}
