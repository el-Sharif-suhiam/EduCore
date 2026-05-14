using System;
using System.Collections.Generic;
using System.Text;
using Common.Dtos;
using Common.Enums;
using EduCore_DataAccess;
namespace EduCore_BusinessLayer
{
    public class clsUsersRoles
    {
        public static async Task<bool> RegistInstructor(int userId)
        {
            return await clsUserRolesData.AddRoleToUser(userId, enRoles.Instructor);
        }

        public static async Task<bool> RemoveInstructor(int userId) { 
            return await clsUserRolesData.RemoveRoleFromUser(userId, enRoles.Instructor);
        }

        public static async Task<bool> AddAdmin(int userId) {
            return await clsUserRolesData.AddRoleToUser(userId, enRoles.Admin);
        }

        public static async Task<bool> RemoveAdmin(int userId) {
            return await clsUserRolesData.RemoveRoleFromUser(userId, enRoles.Admin);
        }

        public static async Task<bool> IsUserAdmin(int userId) {
            return await clsUserRolesData.IsUserAdmin(userId);
        }

        public static async Task<bool> IsUserInstructor(int userId) {
            return await clsUserRolesData.IsUserInstructor(userId);
        }
    }
}
