using Common.Exceptions;
using Common.Utils;
using Common.ViewModels;
using EduCoreAPI.Helpers;
using EduCoreAPI.Helpers.Dtos.RequestDto;
using EduCore_BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCoreAPI.Controllers
{
    // ============================================================
    // Enrollments of the CURRENT authenticated user (L9).
    // A user can only ever read their own enrollments here — there
    // is deliberately no {userId} route variant.
    // ============================================================
    [Route("api/enrollments")]
    [ApiController]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private int CurrentUserId
        {
            get
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int currentUserId))
                    throw new UnauthorizedAccessException();

                return currentUserId;
            }
        }

        // =========================
        // GET: My active enrollments (paged)
        // =========================
        [HttpGet("my")]
        public async Task<ActionResult<List<EnrollmentViewModel>>> GetMyEnrollments(
            [FromQuery] PageRequest pageRequest)
        {
            clsApiValidators.ValidatePaging(pageRequest);

            var enrollments = await clsEnrollment.GetUserEnrollments(
                CurrentUserId,
                pageRequest.PageNumber,
                pageRequest.PageSize);

            return Ok(enrollments);
        }
    }
}
