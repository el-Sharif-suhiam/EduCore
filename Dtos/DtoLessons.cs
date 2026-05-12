using System;
using System.Collections.Generic;
using System.Text;

namespace Dtos
{
    public class DtoLessons
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string Title { get; set; }

        public string VideoUrl { get; set; }

        public string BodyText { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedById { get; set; }

        public int InstructorId { get; set; }
        public int? CourseId { get; set; }
    }
}
