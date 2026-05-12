using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    public class CourseProgressViewModel
    {
        public int CourseId { get; set; }
        public int UserId { get; set; }

        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }

        public double ProgressPercentage { get; set; }

        public List<LessonProgressVM> Lessons { get; set; }
    }

    public class LessonProgressVM
    {
        public int LessonId { get; set; }
        public string Title { get; set; }

        public bool IsComplete { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
