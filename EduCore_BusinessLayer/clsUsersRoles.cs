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
        public static bool RegistInstructor(int userId)
        {
            return clsUserRolesData.AddRoleToUser(userId, enRoles.Instructor);
        }

        public static bool RemoveInstructor(int userId) { 
            return clsUserRolesData.RemoveRoleFromUser(userId, enRoles.Instructor);
        }

        public static bool AddAdmin(int userId) {
            return clsUserRolesData.AddRoleToUser(userId, enRoles.Admin);
        }

        public static bool RemoveAdmin(int userId) {
            return clsUserRolesData.RemoveRoleFromUser(userId, enRoles.Admin);
        }

        public static bool IsUserAdmin(int userId) {
            return clsUserRolesData.IsUserAdmin(userId);
        }

        public static bool IsUserInstructor(int userId) {
            return clsUserRolesData.IsUserInstructor(userId);
        }
    }
}
