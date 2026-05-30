using EduCore_BusinessLayer;

namespace EduCoreAPI.Helpers.Dtos.ResponeDto
{
    public class LessonResponse
    {
        public int Id{ get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal BasePrice { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsPublished { get; set; }
        public string? Summary { get; set; }
        public string Title { get; set; }
        public string? VideoUrl { get; set; }
        public string? BodyText { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int InstructorId { get; set; }
        public int? CourseId { get; set; }
    }
}
