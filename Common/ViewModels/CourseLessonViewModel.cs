
namespace Common.ViewModels
{
    public class CourseLessonViewModel
    {

        public int Id { get; set; }           
        public int ProductId { get; set; }    
        public string Title { get; set; }     
        public string? VideoUrl { get; set; } 
        public string? BodyText { get; set; } 
        public int? InstructorId { get; set; }
        public int CourseId { get; set; }     
    }
}
