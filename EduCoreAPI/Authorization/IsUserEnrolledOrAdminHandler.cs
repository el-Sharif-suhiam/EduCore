using EduCore_BusinessLayer;
using EduCoreAPI.Authorization.Resources;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduCoreAPI.Authorization
{
    public class IsUserEnrolledOrAdminHandler : AuthorizationHandler<IsUserEnrolledOrAdminRequirement, int>
    {
        protected override async Task HandleRequirementAsync(
     AuthorizationHandlerContext context,
     IsUserEnrolledOrAdminRequirement requirement,
     int productId)
        {
            if (context.User.IsInRole("Admin") ||
                context.User.IsInRole("SuperAdmin"))
            {
                context.Succeed(requirement);
                return;
            }

            var userId =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int currentUserId))
                return;

            if (await clsEnrollment.IsUserEnrolled(currentUserId, productId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
