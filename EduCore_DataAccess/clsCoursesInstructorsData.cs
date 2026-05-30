using Common.Dtos;
using Common.Enums;
using Common.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsCoursesInstructorsData
    {
        public static async Task<bool> AddInstructorToCourse(int courseId,int instructorId) { 

            string query = @"INSERT INTO CoursesInstructors (CourseId,InstructorId) 
                                VALUES (@CourseId,@InstructorId);";
            int rowAffected = 0;

            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, conn))
            {
                sqlCommand.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;
                sqlCommand.Parameters.Add("@InstructorId", SqlDbType.Int).Value = instructorId;
                await conn.OpenAsync();
                rowAffected = await sqlCommand.ExecuteNonQueryAsync();
            }

            return (rowAffected > 0);
        }

        public static async Task<bool> RemoveInstructorFromCourse(int courseId, int instructorId)
        {

            string query = @"DELETE FROM CoursesInstructors
                                WHERE CourseId = @CourseId AND InstructorId = @InstructorId;";
            int rowAffected = 0;

            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, conn))
            {
                sqlCommand.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;
                sqlCommand.Parameters.Add("@InstructorId", SqlDbType.Int).Value = instructorId;
                await conn.OpenAsync();
                rowAffected = await sqlCommand.ExecuteNonQueryAsync();
            }

            return (rowAffected > 0);
        }

        public static async Task<List<UsersViewModel>> GetInstructorsForCourseById(int courseId) {

            List<UsersViewModel> users = new List<UsersViewModel>();
            string query = @"SELECT U.Id, U.Name, U.BirthDate, U.Email , U.CreatedAt,
                               R.Name As RoleName,U.IsActive,
                             FROM CoursesInstructors CI
                             JOIN Users U ON U.Id = CI.InstructorId
                             JOIN UserRoles UR ON UR.UserId = U.Id
                             JOIN Roles R ON R.RoleId = UR.RoleId
                             WHERE U.IsActive = 1 AND CI.CourseId = @courseId
                             ORDER BY CreatedAt DESC";
      
            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@courseId", SqlDbType.Int).Value = courseId;

                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {
                    int IdIndex = reader.GetOrdinal("Id");
                    int nameIndex = reader.GetOrdinal("Name");
                    int birthDateIndex = reader.GetOrdinal("BirthDate");
                    int emailIndex = reader.GetOrdinal("Email");
                    int CreatedAtIndex = reader.GetOrdinal("CreatedAt");
                    int isActiveIndex = reader.GetOrdinal("IsActive");
                    int roleNameIndex = reader.GetOrdinal("RoleName");
                    while (await reader.ReadAsync())
                    {

                        users.Add(new UsersViewModel
                        {
                            Id = reader.GetInt32(IdIndex),
                            Name = reader.GetString(nameIndex),
                            BirthDate = reader.GetDateTime(birthDateIndex),
                            Email = reader.GetString(emailIndex),
                            CreatedAt = reader.GetDateTime(CreatedAtIndex),
                            IsActive = reader.GetBoolean(isActiveIndex),
                            Role = reader.GetString(roleNameIndex)
                        });
                    }
                }
            }

            return users;
        
    }

        public static async Task<bool> IsInstructorHasThisCourse(int courseId, int instructorId)
        {

            const string query = @"SELECT CAST(
                                CASE WHEN EXISTS (
                                    CoursesInstructors
                                WHERE CourseId = @CourseId AND InstructorId = @InstructorId
                                )
                                THEN 1 ELSE 0 END
                            AS BIT)";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;
                command.Parameters.Add("@InstructorId", SqlDbType.Int).Value = instructorId;
                await connection.OpenAsync();

                object? result = await command.ExecuteScalarAsync();
                return result != null && (bool)result;
            }
        }

    }
}
