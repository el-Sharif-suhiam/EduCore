namespace EduCoreAPI.Helpers.Models.RequestModels
{
    public record CreatePaymentRequest(
     int OrderId,
     string? PaymentMethod,
     string? DiscountCode,
     string? IdempotencyKey
 );
}
