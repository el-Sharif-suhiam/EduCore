using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using EduCore_BusinessLayer;
using EduCoreAPI.Authorization.Resources;
using EduCoreAPI.Helpers;
using EduCoreAPI.Helpers.Dtos.RequestDto;
using EduCoreAPI.Helpers.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCoreAPI.Controllers
{
    [Route("api/lessons")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        // =========================
        // GET: Independent Lessons
        // Published only for the public storefront.
        // Admin/SuperAdmin may pass includeUnpublished=true to see drafts.
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetIndependentLessons(
            [FromQuery] PageRequest pageRequest, string? search, bool? includeUnpublished)
        {
            clsApiValidators.ValidatePaging(pageRequest);

            bool privileged =
                User.Identity?.IsAuthenticated == true &&
                (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"));

            var lessons = await clsLesson.GetIndependntLessons(
                pageRequest.PageNumber,
                pageRequest.PageSize,
                search,
                privileged && includeUnpublished == true);

            return Ok(lessons);
        }


        // =========================
        // GET: Public lesson info (pre-purchase cover sheet).
        // Never exposes video/body — those stay enrolled/owner/admin-only.
        // =========================
        [AllowAnonymous]
        [HttpGet("{id:int}/info")]
        public async Task<ActionResult<Common.ViewModels.LessonPublicInfoViewModel>> GetLessonPublicInfo([FromRoute] int id)
        {
            var lesson = await clsLesson.GetLessonPublicInfo(id);

            if (lesson is null)
                throw new NotFoundException("Lesson not found");

            return Ok(lesson);
        }

        // =========================
        // GET: by id
        // =========================
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetLessonById([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsLesson lesson = await clsLesson.Find(id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int authenticatedId = int.Parse(userId);

            if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
                return Ok(lessonMapper.ToLessonRespone(lesson));

            bool isInstructor = User.IsInRole("Instructor");
            if (isInstructor)
            {
                bool result = await clsCoursesInstructors.IsInstructorOwnProduct(authenticatedId,enProductType.Lesson , 0, id,0);
                if (result)
                    return Ok(lessonMapper.ToLessonRespone(lesson));
            }

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               lesson.ProductId,
               "IsUserEnrolledOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            return Ok(lessonMapper.ToLessonRespone(lesson));
        }

        // =========================
        // POST: Create Lesson
        // =========================
        [Authorize(Roles ="Instructor,SuperAdmin")]
        [HttpPost]
        public async Task<ActionResult> CreateLesson([FromBody] LessonRequest request)
        {
            var newLesson = new clsLesson();

            newLesson.SetName(request.Name);
            newLesson.SetTitle(request.Title);
            newLesson.SetBasePrice(request.BasePrice);
            newLesson.SetBodyText(request.BodyText);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedtId = int.Parse(userId);


            await newLesson.AssignCreatedByUserAsync(authenticatedtId);
            if (!string.IsNullOrWhiteSpace(request.VideoUrl))
                newLesson.SetVideoUrl(request.VideoUrl);

            if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
                newLesson.SetThumbnailUrl(request.ThumbnailUrl);

            if (!string.IsNullOrWhiteSpace(request.Summary))
                newLesson.SetSummary(request.Summary);

            await newLesson.SetCourseId(null);
            await newLesson.SetInstructorToLesson(authenticatedtId);

            bool result = await newLesson.Save();

            if (!result)
                throw new ConflictException("Failed to create lesson");

            return CreatedAtAction(
                nameof(GetLessonById),
                new { id = newLesson.Id },
                new
                {
                    newLesson.Id,
                    newLesson.Title,
                    newLesson.BodyText,
                    newLesson.BasePrice,
                    newLesson.Summary,
                });
        }

        // =========================
        // PUT: Update Lesson
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPut("{id:int}")]

        public async Task<ActionResult> UpdateLesson(
            [FromRoute] int id,
            [FromBody] LessonRequest request, [FromServices] IAuthorizationService authorizationService)
        {
            clsLesson lesson = await clsLesson.Find(id);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                LessonId = id,
                Type = enProductType.Lesson
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
                lesson.BodyText,
                lesson.BasePrice,
                lesson.Summary,
            });
        }

        // =========================
        // POST: Publish Lesson
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPost("{id:int}/publish")]
        public async Task<ActionResult> PublishLesson([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsLesson lesson = await clsLesson.Find(id);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                LessonId = id,
                Type = enProductType.Lesson
            };

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            bool result = await lesson.PublishLesson();

            if (!result)
                throw new ConflictException("Failed to publish the lesson");

            return Ok(new { success = result });
        }

        // =========================
        // POST: UnPublish Lesson
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPost("{id:int}/unpublish")]
        public async Task<ActionResult> UnPublishLesson([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsLesson lesson = await clsLesson.Find(id);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                LessonId = id,
                Type = enProductType.Lesson
            };

            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            bool result = await lesson.UnPublishLesson();

            if (!result)
                throw new ConflictException("Failed to unpublish the lesson");

            return Ok(new { success = result });
        }

        // =========================
        // DELETE: Lesson
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteLesson(
            [FromRoute] int id,[FromServices] IAuthorizationService authorizationService)
        {
            clsLesson lesson = await clsLesson.Find(id);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                LessonId = id,
                Type = enProductType.Lesson
            };
            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedId = int.Parse(userId);


            bool result = await lesson.Delete(authenticatedId);

            if (!result)
                throw new ConflictException("Failed to delete lesson");

            return Ok(new
            {
                lesson.Id,
                lesson.Title,
                lesson.Name,
                lesson.BasePrice
            });
        }

        // =========================
        // PUT: UnDelete Lesson
        // =========================
        [Authorize(Roles = "Instructor,SuperAdmin")]
        [HttpPut("{id:int}/restore")]
        public async Task<ActionResult> UnDeleteLesson(
            [FromRoute] int id,[FromServices] IAuthorizationService authorizationService)
        {
            clsLesson lesson = await clsLesson.Find(id, includeDeleted: true);

            ProductAccessResource productAccess = new ProductAccessResource
            {
                LessonId = id,
                Type = enProductType.Lesson
            };
            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int authenticatedId = int.Parse(userId);



            bool result = await lesson.UnDelete(authenticatedId);

            if (!result)
                throw new ConflictException("Failed to restore lesson");

            return Ok(new
            {
                lesson.Id,
                lesson.Title,
                lesson.Name,
                lesson.BasePrice
            });
        }
    }
}

