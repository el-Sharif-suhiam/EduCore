using EduCore_BusinessLayer;

namespace EduCoreAPI.Helpers.Dtos.ResponeDto
{
    public class CourseResponse
    {
        public int Id { get; set; }
        public string Name {  get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal BasePrice { get; set; }
        public string CreatedByUser { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsPublished { get; set; }
        public string? Summary { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
