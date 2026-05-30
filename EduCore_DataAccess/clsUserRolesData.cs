using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Common;
using Common.Enums;
namespace EduCore_DataAccess
{
    public class clsUserRolesData
    {
        public static async Task<bool> AddRoleToUser(int UserId, enRoles RoleNum, SqlConnection conn, SqlTransaction tx)
        {

            string query = @"INSERT INTO UserRoles (UserId,RoleId) 
                                VALUES (@userId,@roleId);";
            int rowAffected = 0;

            using (SqlCommand sqlCommand = new SqlCommand(query, conn, tx))
            {
                sqlCommand.Parameters.Add("@userId", SqlDbType.Int).Value = UserId;
                sqlCommand.Parameters.Add("@roleId", SqlDbType.TinyInt).Value = (byte)RoleNum;

                rowAffected = await sqlCommand.ExecuteNonQueryAsync();
            }

            return (rowAffected > 0);
        }
        public static async Task<bool> AddRoleToUser(int userId, enRoles roleNum)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await conn.OpenAsync();
                return await AddRoleToUser(userId, roleNum, conn, null);
            }
        }
        public static async Task<bool> RemoveRoleFromUser(int UserId, enRoles RoleNum)
        {
           
            string query = @"DELETE FROM UserRoles
                            WHERE UserID = @userId AND RoleId = @roleId;";


            int rowAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {


                sqlCommand.Parameters.Add("@userId", SqlDbType.Int).Value = UserId;
                sqlCommand.Parameters.Add("@roleId", SqlDbType.TinyInt).Value = (byte)RoleNum;
                await sqlConnection.OpenAsync();


                rowAffected = await sqlCommand.ExecuteNonQueryAsync();
            }

            return (rowAffected > 0);
            
        }

        private static async Task<bool> CheckUserRole(int userId, enRoles role)
        {
            string query = @"SELECT TOP 1 result = 1  FROM Users U
                            JOIN UserRoles UR ON UR.UserId = Id
                            JOIN Roles R ON R.RoleId = UR.RoleId 
                            WHERE IsActive = 1 AND U.Id = @UserId AND R.Name = @RoleName";
            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@RoleName", SqlDbType.NVarChar).Value = role.ToString();

                await sqlConnection.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();
                return result != null && int.TryParse(result.ToString(), out int exists) && exists == 1;
            }
        }
        public static async Task<bool> IsUserAdmin(int UserId) {
            return await CheckUserRole(UserId, enRoles.Admin);

        }

        public static async Task<bool> IsUserInstructor(int userId)
        {
            return await CheckUserRole(userId, enRoles.Instructor);
        }

        public static async Task<bool> IsUserInstructorOrSuperAdmin(int userId)
        {
            string query = @"SELECT TOP 1 result = 1  FROM Users U
                            JOIN UserRoles UR ON UR.UserId = Id
                            JOIN Roles R ON R.RoleId = UR.RoleId 
                            WHERE IsActive = 1 AND U.Id = @UserId AND R.Name = 'Instructor' AND R.Name = 'SuperAdmin'";
            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                await sqlConnection.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();
                return result != null && int.TryParse(result.ToString(), out int exists) && exists == 1;
            }
        }

    }
}
