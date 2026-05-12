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
        public static bool AddInstructorToCourse(int  courseId, int userId)
        {
            if (clsUsersRoles.IsUserInstructor(userId))
                return clsCoursesInstructorsData.AddInstructorToCourse(courseId, userId);
            else
                return false;
        }

        public static bool RemoveInstructorFromCourse(int courseId, int userId) { 
            return clsCoursesInstructorsData.RemoveInstructorFromCourse(courseId, userId);
        }

        public static List<UsersViewModel> GetAllCourseInstructor(int courseId) {
            return clsCoursesInstructorsData.GetInstructorsForCourseById(courseId);
        }

    }
}
