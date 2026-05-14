using Common.Utils;
using Common.ViewModels;
using EduCore_DataAccess;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduCore_BusinessLayer
{
    public class clsCoursesInstructors
    {
        public static async Task<bool> AddInstructorToCourse(int  courseId, int userId)
        {
            if (await clsUsersRoles.IsUserInstructor(userId))
                return await clsCoursesInstructorsData.AddInstructorToCourse(courseId, userId);
            else
                return false;
        }

        public static async Task<bool> RemoveInstructorFromCourse(int courseId, int userId) { 
            return await clsCoursesInstructorsData.RemoveInstructorFromCourse(courseId, userId);
        }

        public static async Task<List<UsersViewModel>> GetAllCourseInstructor(int courseId) {
            return await clsCoursesInstructorsData.GetInstructorsForCourseById(courseId);
        }

    }
}
