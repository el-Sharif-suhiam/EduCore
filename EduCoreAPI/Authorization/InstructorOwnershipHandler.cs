using Common.Enums;
using EduCore_BusinessLayer;
using EduCoreAPI.Authorization.Resources;
using EduCoreAPI.Helpers.Mappers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduCoreAPI.Authorization
{
    public class InstructorOwnershipHandler : AuthorizationHandler<InstructorOwnershipRequirement, ProductAccessResource>
    {
        protected override async Task HandleRequirementAsync(
     AuthorizationHandlerContext context,
     InstructorOwnershipRequirement requirement,
     ProductAccessResource productAccess)
        {
            if (context.User.IsInRole("SuperAdmin"))
            {
                context.Succeed(requirement);
                return;
            }


            var userId =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int currentUserId))
                return;


            if (context.User.IsInRole("Instructor"))
            {
                bool result = await clsCoursesInstructors.IsInstructorOwnProduct(currentUserId, productAccess.Type,productAccess.CourseId,productAccess.LessonId);
                if (result)
                    context.Succeed(requirement);
            }
        }
    }
    

    
}
