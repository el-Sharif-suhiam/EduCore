using Microsoft.AspNetCore.Authorization;

namespace EduCoreAPI.Authorization
{
    public class IsUserEnrolledOrAdminRequirement : IAuthorizationRequirement
    {
    }
}
