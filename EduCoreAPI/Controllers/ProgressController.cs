using Common.Exceptions;
using Common.ViewModels;
using EduCore_BusinessLayer;
using Microsoft.AspNetCore.Mvc;

namespace EduCoreAPI.Controllers
{
    [Route("api/progress")]
    [ApiController]
    public class ProgressController : ControllerBase
    {
        // =========================
        // GET: Progress For Lesson
        // هل أكمل المستخدم هذا الدرس؟
        // =========================
        [HttpGet("users/{userId:int}/lessons/{lessonId:int}")]
        public async Task<ActionResult> GetLessonProgress(
            [FromRoute] int userId,
            [FromRoute] int lessonId)
        {
            clsProgress progress = await clsProgress.Find(userId, lessonId);

            return Ok(new
            {
                progress.UserId,
                progress.LessonId,
                progress.IsComplete,
                progress.CompletedDate
            });
        }

        // =========================
        // GET: Course Progress
        // نسبة إكمال الكورس كاملاً
        // =========================
        [HttpGet("users/{userId:int}/courses/{courseId:int}")]
        public async Task<ActionResult<CourseProgressViewModel>> GetCourseProgress(
            [FromRoute] int userId,
            [FromRoute] int courseId)
        {
            CourseProgressViewModel progress = await clsProgress.GetCourseProgress(userId, courseId);
            return Ok(progress);
        }

        // =========================
        // GET: Is Course Completed
        // هل أكمل المستخدم الكورس 100%؟
        // =========================
        [HttpGet("users/{userId:int}/courses/{courseId:int}/is-completed")]
        public async Task<ActionResult> IsCourseCompleted(
            [FromRoute] int userId,
            [FromRoute] int courseId)
        {
            bool isCompleted = await clsProgress.IsCourseComplated(userId, courseId);
            return Ok(new { isCompleted });
        }

        // =========================
        // PUT: Mark Lesson As Complete
        // إكمال درس
        // =========================
        [HttpPut("users/{userId:int}/lessons/{lessonId:int}/complete")]
        public async Task<ActionResult> MarkAsComplete(
            [FromRoute] int userId,
            [FromRoute] int lessonId)
        {
            bool result = await clsProgress.MarkAsComplete(userId, lessonId);

            if (!result)
                throw new ConflictException("Failed to mark lesson as complete");

            return Ok(new { success = result });
        }

        // =========================
        // PUT: Mark Lesson As Incomplete
        // إلغاء إكمال درس
        // =========================
        [HttpPut("users/{userId:int}/lessons/{lessonId:int}/incomplete")]
        public async Task<ActionResult> MarkAsIncomplete(
            [FromRoute] int userId,
            [FromRoute] int lessonId)
        {
            bool result = await clsProgress.MarkAsIncomplete(userId, lessonId);

            if (!result)
                throw new ConflictException("Failed to mark lesson as incomplete");

            return Ok(new { success = result });
        }
    }
}