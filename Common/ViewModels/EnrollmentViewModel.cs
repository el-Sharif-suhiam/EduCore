using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    /// <summary>
    /// A learner's active enrollment enriched with display info.
    /// Introduced for L9 ("GET current-user enrollments") — the DAL
    /// previously returned bare DtoEnrollment rows (ids only).
    /// </summary>
    public class EnrollmentViewModel
    {
        public int Id { get; set; }

        // Products-table id (what purchases/enrollment checks use).
        public int ProductId { get; set; }

        public string ProductName { get; set; }
        public int ProductTypeId { get; set; }      // enProductType: Lesson=1, Course=2, Bundle=3
        public string? ThumbnailUrl { get; set; }
        public string? Summary { get; set; }

        public DateTime EnrolledAt { get; set; }
        public DateTime? ExpireAt { get; set; }

        // Deep-link targets (null when the product isn't that type):
        // Courses.Id for course products, Lessons.Id for standalone lessons.
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }
    }
}
