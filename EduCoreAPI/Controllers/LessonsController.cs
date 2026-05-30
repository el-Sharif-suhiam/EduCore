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
        // GET: All Lessons
        // =========================
        //[HttpGet]
        //public async Task<ActionResult<List<DtoLessons>>> GetAllLessons([FromQuery] PageRequest pageRequest)
        //{
        //    clsApiValidators.ValidatePaging(pageRequest);

        //    var lessons = await clsLesson.GetAllLessons(
        //        pageRequest.PageNumber,
        //        pageRequest.PageSize);

        //    return Ok(lessons);
        //}

        // =========================
        // GET: Independent Lessons
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetIndependentLessons([FromQuery] PageRequest pageRequest)
        {
            clsApiValidators.ValidatePaging(pageRequest);

            var lessons = await clsLesson.GetIndependntLessons(
                pageRequest.PageNumber,
                pageRequest.PageSize);

            return Ok(lessons);
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

            var userRole = User.FindFirstValue(ClaimTypes.Role);

            int authenticatedId = int.Parse(userId);


            bool isInstructor =  userRole == "Instructor";
            if (isInstructor)
            {
                bool result = await clsCoursesInstructors.IsInstructorOwnProduct(authenticatedId, id, enProductType.Lesson);
                if (result)
                    return Ok(lessonMapper.ToLessonRespone(lesson));
                else
                    return Forbid();
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
            if (request.VideoUrl is not null)
                newLesson.SetVideoUrl(request.VideoUrl);

            if (request.ThumbnailUrl is not null)
                newLesson.SetThumbnailUrl(request.ThumbnailUrl);

            if (request.Summary is not null)
                newLesson.SetSummary(request.Summary);

            await newLesson.SetCourseId(null);

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
                    newLesson.Name,
                    newLesson.BasePrice
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
                Id = id,
                Type = enProductType.Lesson
            };
            var authResult = await authorizationService.AuthorizeAsync(
               User,
               productAccess,
               "InstructorOwnership");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            if (request.Name is not null)
                lesson.SetName(request.Name);

            if (request.Title is not null)
                lesson.SetTitle(request.Title);

            if (request.BasePrice > 0)
                lesson.SetBasePrice(request.BasePrice);

            if (request.VideoUrl is not null)
                lesson.SetVideoUrl(request.VideoUrl);

            if (request.BodyText is not null)
                lesson.SetBodyText(request.BodyText);

            if (request.ThumbnailUrl is not null)
                lesson.SetThumbnailUrl(request.ThumbnailUrl);

            if (request.Summary is not null)
                lesson.SetSummary(request.Summary);

            bool result = await lesson.Save();

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
                Id = id,
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
                Id = id,
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

