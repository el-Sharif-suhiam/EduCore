namespace EduCoreAPI.Helpers.Models.RequestModels
{
    public record UserUpdateRequest
    (string? Name,
        string? BirthDate,
        string? Email
    );
}
