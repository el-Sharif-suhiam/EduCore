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
        
        private static async Task<DtoUser?> GetUserInternal(string whereClause, SqlParameter parameter)
        {
            DtoUser? user = null;

            string query = $@"SELECT U.Id,U.Name,U.BirthDate,U.Email,U.PasswordHash,
                              U.RefreshTokenHash,U.RefreshTokenExpiresAt,U.RefreshTokenRevokedAt, R.Name AS Role
                              FROM Users U
                              LEFT JOIN UserRoles UR ON UR.UserId = U.Id
                              LEFT JOIN Roles R ON UR.RoleId = R.RoleId
                              WHERE {whereClause} AND U.IsActive = 1;";

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add(parameter);

                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int nameIndex = reader.GetOrdinal("Name");
                    int birthDateIndex = reader.GetOrdinal("BirthDate");
                    int emailIndex = reader.GetOrdinal("Email");
                    int passwordIndex = reader.GetOrdinal("PasswordHash");
                    int refreshHashIndex = reader.GetOrdinal("RefreshTokenHash");
                    int refreshExpIndex = reader.GetOrdinal("RefreshTokenExpiresAt");
                    int revokedIndex = reader.GetOrdinal("RefreshTokenRevokedAt");
                    int roleIndex = reader.GetOrdinal("Role");

                    while (await reader.ReadAsync())
                    {
                        if (user == null)
                        {
                            user = new DtoUser
                            {
                                Id = reader.GetInt32(idIndex),
                                Name = reader.GetString(nameIndex),
                                BirthDate = reader.GetDateTime(birthDateIndex),
                                Email = reader.GetString(emailIndex),
                                PasswordHash = reader.GetString(passwordIndex),
                                RefreshTokenHash = reader.IsDBNull(refreshHashIndex) ? null : reader.GetString(refreshHashIndex),
                                RefreshTokenExpiresAt = reader.IsDBNull(refreshExpIndex) ? null : reader.GetDateTime(refreshExpIndex),
                                RefreshTokenRevokedAt = reader.IsDBNull(revokedIndex) ? null : reader.GetDateTime(revokedIndex),
                                Roles = new List<string>()
                            };
                        }

                        if (!reader.IsDBNull(roleIndex))
                        {
                            string role = reader.GetString(roleIndex);

                            if (!user.Roles.Contains(role))
                                user.Roles.Add(role);
                        }
                    }
                }
            }

            return user;
        }
        public static async Task<DtoUser?> GetUserById(int id)
        {
            return await GetUserInternal(
                "Id = @Id",
                new SqlParameter("@Id", SqlDbType.Int) { Value = id }
            );
        }

        public static async Task<DtoUser?> GetUserByEmail(string email)
        {
            return await GetUserInternal(
                "Email = @Email",
                new SqlParameter("@Email", SqlDbType.NVarChar, 254) { Value = email }
            );
        }
        

        private static async  Task<List<UsersViewModel>> GetAllUsersByPageInternal(int pageNumber, int pageSize, enRoles userRole, bool IncludeNonActive = false, string SearchText = "")
        {
            if (pageNumber < 1) pageNumber = 1;
            if(pageSize <= 0) pageSize = 10;
           
            List<UsersViewModel> users = new List<UsersViewModel>();
            string query = @"SELECT Id, U.Name, BirthDate, Email , U.CreatedAt, R.Name As RoleName, 
                            IsActive
                            FROM Users U
                            JOIN UserRoles UR ON UR.UserId = Id
                            JOIN Roles R ON R.RoleId = UR.RoleId 
                            WHERE (@IsNonActiveIncluded = 1 OR IsActive = 1) AND R.Name Like @RoleName
                            AND ( @SearchText IS NULL OR U.Name LIKE @SearchText 
                            OR Email LIKE @SearchText)
                            ORDER BY CreatedAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";
            //if (IncludeNonActive)
            //{
            //    query = @"SELECT Id, U.Name, BirthDate, Email , U.CreatedAt, R.Name As RoleName, 
            //                IsActive
            //                FROM Users U
            //                JOIN UserRoles UR ON UR.UserId = Id
            //                JOIN Roles R ON R.RoleId = UR.RoleId 
            //                WHERE R.Name = @RoleName
            //                AND ( @SearchName IS NULL OR U.Name LIKE @SearchName)
            //                AND ( @SearchEmail IS NULL OR Email LIKE @SearchEmail)
            //                ORDER BY CreatedAt DESC
            //                OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
            //                FETCH NEXT @RowsPerPage ROWS ONLY;";
            //}

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;

                if (userRole == enRoles.Admin)
                {
                    sqlCommand.Parameters.Add("@RoleName", SqlDbType.NVarChar).Value = "%Admin";

                } else
                    sqlCommand.Parameters.Add("@RoleName", SqlDbType.NVarChar).Value = userRole.ToString();
                
                sqlCommand.Parameters.Add("@SearchText",SqlDbType.NVarChar).Value = String.IsNullOrWhiteSpace(SearchText) ? DBNull.Value : $"%{SearchText}%";
                sqlCommand.Parameters.Add("IsNonActiveIncluded", SqlDbType.Bit).Value = IncludeNonActive;

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

        public static async Task<List<UsersViewModel>> GetAllUsers(int pageNumber, int pageSize, enRoles userRole, string SearchText = "")
            => await GetAllUsersByPageInternal(pageNumber, pageSize,userRole,false,SearchText);

        public static async Task<List<UsersViewModel>> GetAllUsersIncludeNonActive(int pageNumber, int pageSize, enRoles userRole, string SearchText = "")
            => await GetAllUsersByPageInternal(pageNumber, pageSize,userRole, true, SearchText);

        public static async Task<int> AddUser(DtoUser user,SqlConnection conn, SqlTransaction tx)
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

                object result = await sqlCommand.ExecuteScalarAsync();

                UserID = result != null ? Convert.ToInt32(result) : -1;


            }

            return UserID;
            }


        
        public static async Task<bool> UpdateUser(DtoUser user)
        {
            string query = @"UPDATE Users
                            SET Name = @Name, BirthDate = @BirthDate, Email = @Email, 
                            PasswordHash =  @PasswordHash,RefreshTokenHash = @RefreshTokenHash,RefreshTokenExpiresAt = @RefreshTokenExpiresAt,
                            RefreshTokenRevokedAt = @RefreshTokenRevokedAt
                            WHERE Id = @Id And IsActive = 1;";


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
                sqlCommand.Parameters.Add("@RefreshTokenRevokedAt", SqlDbType.DateTime2).Value = (object?)user.RefreshTokenRevokedAt ?? DBNull.Value;
                await sqlConnection.OpenAsync();


                rowAffected = await sqlCommand.ExecuteNonQueryAsync();
            }

            return (rowAffected > 0);
        }
        public static async Task<bool> DeactivateUser(int id)
        {
            string query = @"UPDATE Users
                                SET IsActive = 0
                                WHERE Id = @Id;";


            int rowAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {


                sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                await sqlConnection.OpenAsync();


                rowAffected = await sqlCommand.ExecuteNonQueryAsync();
            }

            return (rowAffected > 0);
        }

        public static async Task<bool> ActivateUser(int id)
        {
            string query = @"UPDATE Users
                                SET IsActive = 1
                                WHERE Id = @Id;";

            int rowAffected = 0;

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                await sqlConnection.OpenAsync();

                rowAffected = await sqlCommand.ExecuteNonQueryAsync();
            }

            return (rowAffected > 0);
        }
        public static async Task<bool> IsEmailExist(string email)
        {
            string query = @"SELECT TOP 1 result = 1 FROM Users WHERE Email = @Email";

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                await sqlConnection.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
        }


    }
}
