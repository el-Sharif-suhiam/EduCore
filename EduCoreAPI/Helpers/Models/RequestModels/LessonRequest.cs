namespace EduCoreAPI.Helpers.Dtos.RequestDto
{
    public record LessonRequest(
    string Name,
    string Title,
    decimal BasePrice,
    string BodyText,
    string? VideoUrl,
    string? ThumbnailUrl,
    string? Summary);
}
