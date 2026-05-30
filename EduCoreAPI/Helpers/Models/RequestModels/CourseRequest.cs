using EduCore_BusinessLayer;

namespace EduCoreAPI.Helpers.Dtos.RequestDto
{
    public record CourseRequest
    (
        string Name,
        decimal BasePrice,
        string? ThumbnailUrl,
        string? Summary,
        string? CoverImageUrl
    );
}
