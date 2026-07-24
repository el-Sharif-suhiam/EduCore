namespace EduCoreAPI.Helpers.Models.RequestModels
{
    public record CourseLessonRequest
    (
    string Name,
    string Title,
    string BodyText,
    string? VideoUrl,
    string? ThumbnailUrl,
    string? Summary);
}
