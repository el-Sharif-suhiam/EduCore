namespace EduCoreAPI.Helpers.Models.RequestModels
{
    public record RefreshRequest
    (
        string RefreshToken,
        string Email
    );
}
