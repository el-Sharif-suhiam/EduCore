using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using Common.ViewModels;
using EduCore_BusinessLayer;
using EduCore_DataAccess;
using EduCoreAPI.Authorization.Resources;
using EduCoreAPI.Helpers;
using EduCoreAPI.Helpers.Dtos.RequestDto;
using EduCoreAPI.Helpers.Mappers;
using EduCoreAPI.Helpers.Models.RequestModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Security.Claims;

namespace EduCoreAPI.Controllers
{
    [Route("api/courses")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        // =========================
        // GET: All Courses
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<DtoCourse>>> GetAllCourses(
            [FromQuery] PageRequest pageRequest,string? search)
        {
            clsApiValidators.ValidatePaging(pageRequest);

            var courses = await clsCourse.GetAllCoursesWithInstructors(
                pageRequest.PageNumber,
                pageRequest.PageSize,false,search);

            return Ok(courses);
        }

        // =========================
        // GET: by id
        // =========================
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetCourseById([FromRoute] int id)
        {
            clsCourse course = await clsCourse.Find(id);
            return Ok(CourseMapper.ToCourseRespone(course));
        }

        // =========================
        // POST: Create Course
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPost]
        public async Task<ActionResult> CreateCourse([FromBody] CourseRequest request)
        {
            var newCourse = new clsCourse();

            newCourse.SetName(request.Name);
            newCourse.SetBasePrice(request.BasePrice);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedStudentId = int.Parse(userId);

            await newCourse.AssignCreatedByUserAsync(authenticatedStudentId);

            if (!string.IsNullOrWhiteSpace(request.Summary))
                newCourse.SetSummary(request.Summary);

            if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
                newCourse.SetThumbnailUrl(request.ThumbnailUrl);

            if (!string.IsNullOrWhiteSpace(request.CoverImageUrl))
                newCourse.SetCoverImageUrl(request.CoverImageUrl);

            bool result = await newCourse.Save();

            if (!result)
                throw new ConflictException("Failed to create course");

            return CreatedAtAction(
                nameof(GetCourseById),
                new { id = newCourse.Id },
                new
                {
                    newCourse.Id,
                    newCourse.Name,
                    newCourse.BasePrice
                    
                });
        }

        // =========================
        // PUT: Update Course
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateCourse([FromRoute] int id,[FromBody] CourseRequest request, [FromServices] IAuthorizationService authorizationService)
        {
            ProductAccessResource productAccess = new ProductAccessResource
            {
                CourseId = id,
                Type = enProductType.Course
            };

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            clsCourse course = await clsCourse.Find(id);

            if (!string.IsNullOrWhiteSpace(request.Name))
                course.SetName(request.Name);

            if (request.BasePrice > 0)
                course.SetBasePrice(request.BasePrice);

            if (!string.IsNullOrWhiteSpace(request.Summary))
                course.SetSummary(request.Summary);

            if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
                course.SetThumbnailUrl(request.ThumbnailUrl);

            if (!string.IsNullOrWhiteSpace(request.CoverImageUrl))
                course.SetCoverImageUrl(request.CoverImageUrl);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedId = int.Parse(userId);

            bool result = await course.Save(authenticatedId);

            if (!result)
                throw new ConflictException("Failed to update course");

            return Ok(new {
                course.Id,
                course.Name,
                course.BasePrice,
                course.Summary,
                course.ThumbnailUrl,
                course.CoverImageUrl
            });
        }
        // =========================
        // POST: Add Lesson To Course
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPost("{id:int}/lessons")]
        public async Task<ActionResult> AddNewLessonToCourse([FromRoute] int id, [FromBody] CourseLessonRequest cLessonRequest,
            [FromServices] IAuthorizationService authorizationService)
        {

            ProductAccessResource productAccess = new ProductAccessResource
            {
                CourseId = id,
                Type = enProductType.Course
            };

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            //clsCourse course = await clsCourse.Find(id);
            clsLesson newLesson = new clsLesson();
            newLesson.SetName(cLessonRequest.Name);
            newLesson.SetTitle(cLessonRequest.Title);
            newLesson.SetBasePrice(0);
            newLesson.SetBodyText(cLessonRequest.BodyText);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedId = int.Parse(userId);

            await newLesson.AssignCreatedByUserAsync(authenticatedId);
            if (!string.IsNullOrWhiteSpace(cLessonRequest.VideoUrl))
                newLesson.SetVideoUrl(cLessonRequest.VideoUrl);

            if (!string.IsNullOrWhiteSpace(cLessonRequest.ThumbnailUrl))
                newLesson.SetThumbnailUrl(cLessonRequest.ThumbnailUrl);

            if (!string.IsNullOrWhiteSpace(cLessonRequest.Summary))
                newLesson.SetSummary(cLessonRequest.Summary);

            await newLesson.SetInstructorToLesson(authenticatedId);
            await newLesson.SetCourseId(id);

            bool makeLessonResult = await newLesson.Save();

            if (!makeLessonResult)
                throw new ConflictException("Failed to create lesson");

            return Ok(new
            {
                newLesson.Id, 
                newLesson.Name,
                newLesson.Title,
                newLesson.CourseId
            });
        }

        // =========================
        // PUT: Update Course Lesson
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPut("{courseId:int}/lessons/{lessonId}")]

        public async Task<ActionResult> UpdateLesson(
            [FromRoute] int courseId,
            [FromRoute] int lessonId,
            [FromBody] LessonRequest request, [FromServices] IAuthorizationService authorizationService)
        {
            clsLesson lesson = await clsLesson.FindWithCourseId(lessonId ,courseId);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                LessonId = lessonId,
                CourseId = courseId,
                Type = enProductType.CourseLesson
            };
            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            if (!string.IsNullOrWhiteSpace(request.Name))
                lesson.SetName(request.Name);

            if (!string.IsNullOrWhiteSpace(request.Title))
                lesson.SetTitle(request.Title);

            if (request.BasePrice > 0)
                lesson.SetBasePrice(request.BasePrice);

            if (!string.IsNullOrWhiteSpace(request.VideoUrl))
                lesson.SetVideoUrl(request.VideoUrl);

            if (!string.IsNullOrWhiteSpace(request.BodyText))
                lesson.SetBodyText(request.BodyText);

            if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
                lesson.SetThumbnailUrl(request.ThumbnailUrl);

            if (!string.IsNullOrWhiteSpace(request.Summary))
                lesson.SetSummary(request.Summary);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedId = int.Parse(userId);

            bool result = await lesson.Save(authenticatedId);

            if (!result)
                throw new ConflictException("Failed to update lesson");

            return Ok(new
            {
                lesson.Id,
                lesson.Title,
                lesson.Name,
                lesson.BasePrice
            });
        }


        // =========================
        // GET: Get all lessons for Course
        // =========================
        [AllowAnonymous]
        [HttpGet("{id:int}/lessons")]
        public async Task<ActionResult> GetLessonsByCourse([FromRoute] int id)
        {
            var lessons = await clsLesson.GetLessonsByCourse(id);
            return Ok(lessons);
        }
       
        // =========================
        // POST: Publish Course
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPost("{id:int}/publish")]
        public async Task<ActionResult> PublishCourse([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsCourse course = await clsCourse.Find(id);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                CourseId = id,
                Type = enProductType.Course
            };

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            bool result = await course.PublishCourse();

            if (!result)
                throw new ConflictException("Failed to publish the course");

            return Ok(new { success = result });
        }

        // =========================
        // POST: UnPublish Course
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPost("{id:int}/unpublish")]
        public async Task<ActionResult> UnPublishCourse([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsCourse course = await clsCourse.Find(id);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                CourseId = id,
                Type = enProductType.Course
            };

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            bool result = await course.UnPublishCourse();

            if (!result)
                throw new ConflictException("Failed to unpublish the course");

            return Ok(new { success = result });
        }

        // =========================
        // GET: Course Exists
        // =========================
        [HttpGet("{id:int}/exists")]
        public async Task<ActionResult> CourseExists([FromRoute] int id)
        {
            var exists = await clsCourse.IsCourseExist(id);
            return Ok(new { exists });
        }

        // =========================
        // POST: Assign Instrucctor To Course
        // =========================
        [Authorize(Roles ="Admin,SuperAdmin")]
        [HttpPost("{id:int}/instructors")]
        public async Task<ActionResult> AssginInstructorToCourse([FromRoute] int id, [FromQuery] int instructorId)
        {
            clsUser user = await clsUser.Find(instructorId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);

            bool result = await clsCoursesInstructors.AddInstructorToCourse(id, instructorId,actionbyId);

            if (!result)
                throw new ConflictException("error in assgin instructor to course");

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email
            });
        }

        // =========================
        // GET: GET Course Instructors
        // =========================
        [Authorize(Roles ="Admin,SuperAdmin")]
        [HttpGet("{courseId:int}/instructors")]
        public async Task<ActionResult> GetCourseInstructors([FromRoute] int courseId)
        {
            var instructors = await clsCoursesInstructors.GetAllCourseInstructor(courseId); 
            return Ok(instructors);
        }

        // =========================
        // DELETE: DELETE Instructor From Course
        // =========================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{courseId:int}/instructors")]
        public async Task<ActionResult> RemoveInstructorFromCourse([FromRoute] int courseId, [FromQuery] int instructorId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int actionbyId = int.Parse(userId);

            bool result = await clsCoursesInstructors.RemoveInstructorFromCourse(courseId,instructorId,actionbyId);
            if (!result)
                throw new ConflictException("something went wrong while deleting the instructor");
            return Ok(new { success = result });
        }



    }
}

