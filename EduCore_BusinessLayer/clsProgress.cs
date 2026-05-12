using Common.Dtos;
using Common.Exceptions;
using Common.ViewModels;
using EduCore_DataAccess;
using System;
using System.ComponentModel.DataAnnotations;

namespace EduCore_BusinessLayer
{
    public class clsProgress
    {
        private DtoProgress _ProgressData;

        public int UserId => _ProgressData.UserId;
        public int LessonId => _ProgressData.LessonId;
        public bool? IsComplete => _ProgressData.IsComplete;
        public DateTime? CompletedDate => _ProgressData.CompletedDate;

        
        public clsProgress(int userId, int lessonId)
        {
            if (userId <= 0)
                throw new ValidationException("User id is not valid");
            if (lessonId <= 0)
                throw new ValidationException("Lesson id is not valid");

            _ProgressData = new DtoProgress
            {
                UserId = userId,
                LessonId = lessonId,
            };
        }

        private clsProgress(DtoProgress progress)
        {
            _ProgressData = progress;
        }


        public void SetComplete(bool isComplete)
        {
            _ProgressData.IsComplete = isComplete;
        }


        public static clsProgress Find(int userId, int lessonId)
        {
            if (userId <= 0)
                throw new ValidationException("User id is not valid");
            if (lessonId <= 0)
                throw new ValidationException("Lesson id is not valid");

            DtoProgress dto = clsProgressData.GetProgress(userId, lessonId);

            if (dto is null)
                throw new NotFoundException("No progress found for this user and lesson");

            return new clsProgress(dto);
        }


        public bool Save()
        {
            return clsProgressData.UpsertProgress(_ProgressData);
        }


        public static bool MarkAsComplete(int userId, int lessonId)
        {
            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(true);
            return progress.Save();
        }

        public static bool MarkAsIncomplete(int userId, int lessonId)
        {
            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(false);
            return progress.Save();
        }

        public static CourseProgressViewModel GetCourseProgress(int userId, int courseId)
        {
            if (!clsCoursesData.CourseExists(courseId))
                throw new NotFoundException("Course not found");

            return clsProgressData.GetCourseProgress(userId, courseId);
        }

        public static bool IsCourseComplated(int userId, int courseId)
        {
            CourseProgressViewModel courseProgress = GetCourseProgress(userId, courseId);
            return courseProgress.ProgressPercentage == 100;
        }
        //////////////////////////////////////////////////////// تذكير بكتابة منطق لكتابة الشهادة
        /// اول شيء كلاس يراجع هل اكمل الكورس 
        /// بعدها جدول فيه الشهادات بالتاريخ واسم الشهادة ب uuid 
        /// وكلاس لانشاء ملف pdf فيه بيانات الشهادة
    }
}