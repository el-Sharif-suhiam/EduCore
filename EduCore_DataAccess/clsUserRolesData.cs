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
        public static bool AddRoleToUser(int UserId, enRoles RoleNum, SqlConnection conn, SqlTransaction tx)
        {

            string query = @"INSERT INTO UserRoles (UserId,RoleId) 
                                VALUES (@userId,@roleId);";
            int rowAffected = 0;

            using (SqlCommand sqlCommand = new SqlCommand(query, conn, tx))
            {
                sqlCommand.Parameters.Add("@userId", SqlDbType.Int).Value = UserId;
                sqlCommand.Parameters.Add("@roleId", SqlDbType.TinyInt).Value = (byte)RoleNum;

                rowAffected = sqlCommand.ExecuteNonQuery();
            }

            return (rowAffected > 0);
        }
        public static bool AddRoleToUser(int userId, enRoles roleNum)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                conn.Open();
                return AddRoleToUser(userId, roleNum, conn, null);
            }
        }
        public static bool RemoveRoleFromUser(int UserId, enRoles RoleNum)
        {
           
            string query = @"DELETE FROM UserRoles
                            WHERE UserID = @userId AND RoleId = @roleId;";


            int rowAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {


                sqlCommand.Parameters.Add("@userId", SqlDbType.Int).Value = UserId;
                sqlCommand.Parameters.Add("@roleId", SqlDbType.TinyInt).Value = (byte)RoleNum;
                sqlConnection.Open();


                rowAffected = sqlCommand.ExecuteNonQuery();
            }

            return (rowAffected > 0);
            
        }

        private static bool CheckUserRole(int userId, enRoles role)
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

                sqlConnection.Open();
                object result = cmd.ExecuteScalar();
                return result != null && int.TryParse(result.ToString(), out int exists) && exists == 1;
            }
        }
        public static bool IsUserAdmin(int UserId) {
            return CheckUserRole(UserId, enRoles.Admin);

        }

        public static bool IsUserInstructor(int userId)
        {
            return CheckUserRole(userId, enRoles.Instructor);
        }
    }
}
