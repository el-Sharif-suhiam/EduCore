using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoProgress
    {
        public int UserId { get; set; }
        public int LessonId { get; set; }

        public DateTime? CompletedDate { get; set; }
        public bool? IsComplete { get; set; }
    }
}
