using Common.Enums;
using Common.Utils;
using Common.ViewModels;
using EduCore_DataAccess;

namespace EduCore_BusinessLayer
{
    public class clsCoursesInstructors
    {
 
        public static async Task<bool> AddInstructorToCourse(
            int courseId,
            int userId,
            int actionByUserId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            clsValidation.ValidatePositiveInt(courseId, "CourseId");
            clsValidation.ValidatePositiveInt(userId, "UserId");
            clsValidation.ValidatePositiveInt(actionByUserId, "ActionByUserId");

            bool isInstructor =
                await clsUsersRoles.IsUserInstructor(userId);

            if (!isInstructor)
                return false;

            bool result =
                await clsCoursesInstructorsData.AddInstructorToCourse(
                    courseId,
                    userId);

            if (!result)
                return false;

            await clsAudit.LogAsync(
                actionByUserId,
                enAuditActionType.UpdateCourse,
                "Course",
                courseId,
                $"Assigned instructor id {userId} to course id {courseId}",
                ipAddress,
                userAgent);

            return true;
        }

        public static async Task<bool> RemoveInstructorFromCourse(
            int courseId,
            int userId,
            int actionByUserId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            clsValidation.ValidatePositiveInt(courseId, "CourseId");
            clsValidation.ValidatePositiveInt(userId, "UserId");
            clsValidation.ValidatePositiveInt(actionByUserId, "ActionByUserId");

            bool result =
                await clsCoursesInstructorsData.RemoveInstructorFromCourse(
                    courseId,
                    userId);

            if (!result)
                return false;

            await clsAudit.LogAsync(
                actionByUserId,
                enAuditActionType.UpdateCourse,
                "Course",
                courseId,
                $"Removed instructor id {userId} from course id {courseId}",
                ipAddress,
                userAgent);

            return true;
        }


        public static async Task<List<UsersViewModel>>
            GetAllCourseInstructor(int courseId)
        {
            clsValidation.ValidatePositiveInt(courseId, "CourseId");

            return await clsCoursesInstructorsData
                .GetInstructorsForCourseById(courseId);
        }


        public static async Task<bool> IsInstrctorHasThisCourse(
            int courseId,
            int userId)
        {
            clsValidation.ValidatePositiveInt(courseId, "CourseId");
            clsValidation.ValidatePositiveInt(userId, "UserId");

            return await clsCoursesInstructorsData
                .IsInstructorHasThisCourse(courseId, userId);
        }

        public static async Task<bool> IsInstructorOwnProduct(
        int userId,
        enProductType productType,
        int courseId,
        int lessonId,
        int bundleId
        )
        {
            switch (productType)
            {
                case enProductType.Lesson:
                    clsLesson lesson = await clsLesson.Find(lessonId);
                    return lesson.InstructorId == userId;

                case enProductType.Course:
                    return await IsInstrctorHasThisCourse(courseId, userId);

                case enProductType.CourseLesson:
                    clsLesson courseLesson = await clsLesson.FindWithCourseId(lessonId, courseId);
                    return courseLesson.InstructorId == userId;

                case enProductType.Bundle:
                    clsBundle bundle = await clsBundle.Find(bundleId);
                    return bundle.CreatedByUser.Id == userId;
                default:
                    return false;
            }
        }
    }
}