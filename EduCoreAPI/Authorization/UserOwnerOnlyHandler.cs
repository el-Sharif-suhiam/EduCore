using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduCoreAPI.Authorization
{
    public class UserOwnerOnlyHandler
        : AuthorizationHandler<UserOwnerOnlyRequirement, int>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UserOwnerOnlyRequirement requirement,
            int userId)
        {
            var authenticatedUserId =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(authenticatedUserId, out int currentUserId))
            {
                if (currentUserId == userId)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}