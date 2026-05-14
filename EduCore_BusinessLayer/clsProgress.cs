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


        public static async Task<clsProgress> Find(int userId, int lessonId)
        {
            if (userId <= 0)
                throw new ValidationException("User id is not valid");
            if (lessonId <= 0)
                throw new ValidationException("Lesson id is not valid");

            DtoProgress dto = await clsProgressData.GetProgress(userId, lessonId);

            if (dto is null)
                throw new NotFoundException("No progress found for this user and lesson");

            return new clsProgress(dto);
        }


        public async Task<bool> Save()
        {
            return await clsProgressData.UpsertProgress(_ProgressData);
        }


        public static async Task<bool> MarkAsComplete(int userId, int lessonId)
        {
            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(true);
            return await progress.Save();
        }

        public static async Task<bool> MarkAsIncomplete(int userId, int lessonId)
        {
            clsProgress progress = new clsProgress(userId, lessonId);
            progress.SetComplete(false);
            return await progress.Save();
        }

        public static async Task<CourseProgressViewModel> GetCourseProgress(int userId, int courseId)
        {
            if (!(await clsCoursesData.CourseExists(courseId)))
                throw new NotFoundException("Course not found");

            return await clsProgressData.GetCourseProgress(userId, courseId);
        }

        public static async Task<bool> IsCourseComplated(int userId, int courseId)
        {
            CourseProgressViewModel courseProgress = await GetCourseProgress(userId, courseId);
            return courseProgress.ProgressPercentage == 100;
        }
        //////////////////////////////////////////////////////// تذكير بكتابة منطق لكتابة الشهادة
        /// اول شيء كلاس يراجع هل اكمل الكورس 
        /// بعدها جدول فيه الشهادات بالتاريخ واسم الشهادة ب uuid 
        /// وكلاس لانشاء ملف pdf فيه بيانات الشهادة
    }
}