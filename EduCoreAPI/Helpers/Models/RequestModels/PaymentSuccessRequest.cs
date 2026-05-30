namespace EduCoreAPI.Helpers.Models.RequestModels
{
    public record PaymentSuccessRequest(
     int UserId,
     string TransactionId,
     DateTime? PaidAt
 );

}
