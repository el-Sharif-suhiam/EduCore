using System;

namespace Common.ViewModels
{
    // Public "cover sheet" for a standalone lesson shown BEFORE purchase.
    // Deliberately tiny: never carries VideoUrl/BodyText (content stays
    // gated behind enrolled/owner/admin via GET /api/lessons/{id}).
    public class LessonPublicInfoViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string? Summary { get; set; }
        public decimal BasePrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int InstructorId { get; set; }
        public string InstructorName { get; set; }
    }
}