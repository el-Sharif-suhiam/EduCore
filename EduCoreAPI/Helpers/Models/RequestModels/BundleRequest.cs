namespace EduCoreAPI.Helpers.Dtos.RequestDto
{
    public record BundleRequest
    (
        string Name,
        decimal BasePrice,
        string Summary,
        string ThumbnailUrl
    );
}
