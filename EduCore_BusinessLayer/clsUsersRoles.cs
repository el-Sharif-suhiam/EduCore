using Common.Enums;
using Common.Exceptions;
using EduCore_DataAccess;
using System.ComponentModel.DataAnnotations;

namespace EduCore_BusinessLayer
{
    public class clsUsersRoles
    {
       

        public static async Task<bool> RegistInstructor(
            int userId,
            int actionByUserId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (userId <= 0)
                throw new ValidationException("Invalid userId");

            bool result =
                await clsUserRolesData.AddRoleToUser(userId, enRoles.Instructor);

            if (!result)
                return false;

            await clsAudit.LogAsync(
                actionByUserId,
                enAuditActionType.UserPromotedToInstructor,
                "UserRole",
                userId,
                $"User {userId} promoted to Instructor",
                ipAddress,
                userAgent);

            return result;
        }

        public static async Task<bool> RemoveInstructor(
            int userId,
            int actionByUserId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            bool result =
                await clsUserRolesData.RemoveRoleFromUser(userId, enRoles.Instructor);

            if (!result)
                return false;

            await clsAudit.LogAsync(
                actionByUserId,
                enAuditActionType.UserRemovedFromInstructors,
                "UserRole",
                userId,
                $"Instructor role removed from user {userId}",
                ipAddress,
                userAgent);

            return result;
        }



        public static async Task<bool> AddAdmin(
            int userId,
            int actionByUserId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            bool result =
                await clsUserRolesData.AddRoleToUser(userId, enRoles.Admin);

            if (!result)
                return false;

            await clsAudit.LogAsync(
                actionByUserId,
                enAuditActionType.UserPromotedToAdmin,
                "UserRole",
                userId,
                $"User {userId} promoted to Admin",
                ipAddress,
                userAgent);

            return result;
        }

        public static async Task<bool> RemoveAdmin(
            int userId,
            int actionByUserId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            bool result =
                await clsUserRolesData.RemoveRoleFromUser(userId, enRoles.Admin);

            if (!result)
                return false;

            await clsAudit.LogAsync(
                actionByUserId,
                enAuditActionType.UserRemovedFromAdmins,
                "UserRole",
                userId,
                $"Admin role removed from user {userId}",
                ipAddress,
                userAgent);

            return result;
        }


        public static Task<bool> IsUserAdmin(int userId)
        {
            return clsUserRolesData.IsUserAdmin(userId);
        }

        public static Task<bool> IsUserInstructor(int userId)
        {
            return clsUserRolesData.IsUserInstructor(userId);
        }

        public static Task<bool> IsUserInstructorOrSuperAdmin(int userId)
        {
            return clsUserRolesData.IsUserInstructorOrSuperAdmin(userId);
        }
    }
}