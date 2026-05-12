using Common;
using Common.Dtos;
using Common.Enums;
using Common.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Xml.Linq;
namespace EduCore_DataAccess
{
    public class clsUsersData
    {
        private static DtoUser GetUserInternal(string whereClause, SqlParameter parameter)
        {
            DtoUser user = null;

            string query = $@"SELECT Id, Name, BirthDate, Email, PasswordHash, 
                                     RefreshTokenHash, RefreshTokenExpiresAt, RefreshTokenRevokedAt
                              FROM Users
                              WHERE {whereClause} AND IsActive = 1";

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add(parameter);

                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int nameIndex = reader.GetOrdinal("Name");
                        int birthDateIndex = reader.GetOrdinal("BirthDate");
                        int emailIndex = reader.GetOrdinal("Email");
                        int passwordIndex = reader.GetOrdinal("PasswordHash");
                        int refreshHashIndex = reader.GetOrdinal("RefreshTokenHash");
                        int refreshExpIndex = reader.GetOrdinal("RefreshTokenExpiresAt");
                        int revokedIndex = reader.GetOrdinal("RefreshTokenRevokedAt");

                        user = new DtoUser
                        {
                            Id = reader.GetInt32(idIndex),
                            Name = reader.GetString(nameIndex),
                            BirthDate = reader.GetDateTime(birthDateIndex),
                            Email = reader.GetString(emailIndex),
                            PasswordHash = reader.GetString(passwordIndex),
                            RefreshTokenHash = reader.GetString(refreshHashIndex),
                            RefreshTokenExpiresAt = reader.GetDateTime(refreshExpIndex),
                            RefreshTokenRevokedAt = reader.IsDBNull(revokedIndex)
                                ? null
                                : reader.GetDateTime(revokedIndex),
                        };
                    }
                }
            }

            return user;
        }
        public static DtoUser GetUserById(int id)
        {
            return GetUserInternal(
                "Id = @Id",
                new SqlParameter("@Id", SqlDbType.Int) { Value = id }
            );
        }

        public static DtoUser GetUserByEmail(string email)
        {
            return GetUserInternal(
                "Email = @Email",
                new SqlParameter("@Email", SqlDbType.NVarChar, 254) { Value = email }
            );
        }
        

        private static  List<UsersViewModel> GetAllUsersByPageInternal(int pageNumber, int pageSize, enRoles userRole, bool IncludeNonActive = false)
        {
            if (pageNumber < 1) pageNumber = 1;
            if(pageSize <= 0) pageSize = 10;

            List<UsersViewModel> users = new List<UsersViewModel>();
            string query = @"SELECT Id, U.Name, BirthDate, Email , U.CreatedAt, R.Name As RoleName, 
                            IsActive
                            FROM Users U
                            JOIN UserRoles UR ON UR.UserId = Id
                            JOIN Roles R ON R.RoleId = UR.RoleId 
                            WHERE IsActive = 1 AND R.Name = @RoleName
                            ORDER BY CreatedAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";
            if (IncludeNonActive)
            {
                query = @"SELECT Id, U.Name, BirthDate, Email , U.CreatedAt, R.Name As RoleName, 
                            IsActive
                            FROM Users U
                            JOIN UserRoles UR ON UR.UserId = Id
                            JOIN Roles R ON R.RoleId = UR.RoleId 
                            WHERE R.Name = @RoleName
                            ORDER BY CreatedAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";
            }

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                sqlCommand.Parameters.Add("@RoleName", SqlDbType.NVarChar).Value = userRole.ToString();

                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    int IdIndex = reader.GetOrdinal("Id");
                    int nameIndex = reader.GetOrdinal("Name");
                    int birthDateIndex = reader.GetOrdinal("BirthDate");
                    int emailIndex = reader.GetOrdinal("Email");
                    int CreatedAtIndex = reader.GetOrdinal("CreatedAt");
                    int isActiveIndex = reader.GetOrdinal("IsActive");
                    int roleNameIndex = reader.GetOrdinal("RoleName");
                    while (reader.Read())
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

        public static List<UsersViewModel> GetAllUsers(int pageNumber, int pageSize, enRoles userRole)
            => GetAllUsersByPageInternal(pageNumber, pageSize,userRole);

        public static List<UsersViewModel> GetAllUsersIncludeNonActive(int pageNumber, int pageSize, enRoles userRole)
            => GetAllUsersByPageInternal(pageNumber, pageSize,userRole, true);

        public static  int AddUser(DtoUser user,SqlConnection conn, SqlTransaction tx)
            {
                int UserID = -1;

                string query = @"INSERT INTO Users (Name,BirthDate,
                                Email,PasswordHash,RefreshTokenHash,RefreshTokenExpiresAt,RefreshTokenRevokedAt) 
                                VALUES (@Name,@BirthDate, @Email,@PasswordHash, @RefreshTokenHash,@RefreshTokenExpiresAt, @RefreshTokenRevokedAt)
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";


                using (SqlCommand sqlCommand = new SqlCommand(query, conn,tx))
                {
                    sqlCommand.Parameters.Add("@Name", SqlDbType.NVarChar).Value = user.Name;
                    sqlCommand.Parameters.Add("@BirthDate", SqlDbType.Date).Value = user.BirthDate;
                    sqlCommand.Parameters.Add("@Email", SqlDbType.NVarChar).Value = user.Email;
                    sqlCommand.Parameters.Add("@PasswordHash", SqlDbType.NVarChar).Value =  user.PasswordHash;
                    sqlCommand.Parameters.Add("@RefreshTokenHash", SqlDbType.NVarChar).Value = user.RefreshTokenHash;
                    sqlCommand.Parameters.Add("@RefreshTokenExpiresAt", SqlDbType.DateTime2).Value = user.RefreshTokenExpiresAt;
                sqlCommand.Parameters.Add("@RefreshTokenRevokedAt", SqlDbType.DateTime2).Value = (object?)user.RefreshTokenRevokedAt ?? DBNull.Value;

                object result = sqlCommand.ExecuteScalar();

                UserID = result != null ? Convert.ToInt32(result) : -1;


            }

            return UserID;
            }


        
        public static  bool UpdateUser(DtoUser user)
        {
            string query = @"UPDATE Users
                            SET Name = @Name, BirthDate = @BirthDate, Email = @Email, 
                            PasswordHash =  @PasswordHash,RefreshTokenHash = @RefreshTokenHash,RefreshTokenExpiresAt = @RefreshTokenExpiresAt,
                            RefreshTokenRevokedAt = @RefreshTokenRevokedAt
                            WHERE UserID = @Id And IsActive = 1;";


            int rowAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {


                sqlCommand.Parameters.Add("@Id",SqlDbType.Int).Value =  user.Id;
                sqlCommand.Parameters.Add("@Name", SqlDbType.NVarChar).Value = user.Name;
                sqlCommand.Parameters.Add("@BirthDate", SqlDbType.Date).Value = user.BirthDate;
                sqlCommand.Parameters.Add("@Email", SqlDbType.NVarChar).Value = user.Email;
                sqlCommand.Parameters.Add("@PasswordHash", SqlDbType.NVarChar).Value = user.PasswordHash;
                sqlCommand.Parameters.Add("@RefreshTokenHash", SqlDbType.NVarChar).Value = user.RefreshTokenHash;
                sqlCommand.Parameters.Add("@RefreshTokenExpiresAt", SqlDbType.DateTime2).Value = user.RefreshTokenExpiresAt;
                sqlCommand.Parameters.Add("@RefreshTokenRevokedAt", SqlDbType.DateTime2).Value = user.RefreshTokenRevokedAt;
                sqlConnection.Open();


                rowAffected = sqlCommand.ExecuteNonQuery();
            }

            return (rowAffected > 0);
        }
        public static  bool DeactivateUser(int id)
        {
            string query = @"UPDATE Users
                                SET IsActive = 0
                                WHERE UserID = @Id;";


            int rowAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {


                sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                sqlConnection.Open();


                rowAffected = sqlCommand.ExecuteNonQuery();
            }

            return (rowAffected > 0);
        }
        public static  bool IsEmailExist(string email)
        {
            string query = @"SELECT TOP 1 result = 1 FROM Users WHERE Email = @Email";

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                sqlConnection.Open();
                object result = cmd.ExecuteScalar();
                return result != null && int.TryParse(result.ToString(), out int exists) && exists == 1;
            }
        }


    }
}
