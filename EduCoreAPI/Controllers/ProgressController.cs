using Common.Exceptions;
using Common.ViewModels;
using EduCore_BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCoreAPI.Controllers
{
    [Route("api/progress")]
    [ApiController]
    [Authorize]
    public class ProgressController : ControllerBase
    {
        private int CurrentUserId
        {
            get
            {
                string? userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int currentUserId))
                    throw new UnauthorizedAccessException();

                return currentUserId;
            }
        }

        // =========================
        // GET: Lesson Progress
        // =========================
        [HttpGet("lessons/{lessonId:int}")]
        public async Task<ActionResult> GetLessonProgress(
            [FromRoute] int lessonId)
        {
            clsProgress progress =
                await clsProgress.Find(
                    CurrentUserId,
                    lessonId);

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
        // =========================
        [HttpGet("courses/{courseId:int}")]
        public async Task<ActionResult<CourseProgressViewModel>>
            GetCourseProgress(
            [FromRoute] int courseId)
        {
            CourseProgressViewModel progress =
                await clsProgress.GetCourseProgress(
                    CurrentUserId,
                    courseId);

            return Ok(progress);
        }

        // =========================
        // GET: Is Course Completed
        // =========================
        [HttpGet("courses/{courseId:int}/is-completed")]
        public async Task<ActionResult>
            IsCourseCompleted(
            [FromRoute] int courseId)
        {
            bool isCompleted =
                await clsProgress.IsCourseComplated(
                    CurrentUserId,
                    courseId);

            return Ok(new
            {
                isCompleted
            });
        }


        [HttpGet("courses/{courseId:int}/certificate")]
        public async Task<ActionResult>
           IssueCertificate(
           [FromRoute] int courseId)
        {
            await clsProgress.IssueCertificate(CurrentUserId, courseId, "api/courses/certificate");
           

            return Ok();
        }
        // =========================
        // PUT: Complete Lesson
        // =========================
        [HttpPut("lessons/{lessonId:int}/complete")]
        public async Task<ActionResult>
            MarkAsComplete(
            [FromRoute] int lessonId)
        {
            bool result =
                await clsProgress.MarkAsComplete(
                    CurrentUserId,
                    lessonId);

            if (!result)
                throw new ConflictException(
                    "Failed to mark lesson as complete");

            return Ok(new
            {
                success = true
            });
        }

        // =========================
        // PUT: Incomplete Lesson
        // =========================
        [HttpPut("lessons/{lessonId:int}/incomplete")]
        public async Task<ActionResult>
            MarkAsIncomplete(
            [FromRoute] int lessonId)
        {
            bool result =
                await clsProgress.MarkAsIncomplete(
                    CurrentUserId,
                    lessonId);

            if (!result)
                throw new ConflictException(
                    "Failed to mark lesson as incomplete");

            return Ok(new
            {
                success = true
            });
        }
    }
}