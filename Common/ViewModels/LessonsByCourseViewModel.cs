using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    public class LessonsByCourseViewModel : LessonsWithOutCoursesViewModel
    {
        public int CourseId { get; set; }
    }
}
