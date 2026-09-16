using Common.Dtos;
using Common.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsEnrollmentsData
    {

        public static async Task<bool> AddEnrollment(DtoEnrollmentDataRequest enrollment, SqlConnection conn, SqlTransaction tx)
        {

            using (SqlCommand command = new SqlCommand("SP_CreateEnrollmentsFromPaidOrder", conn, tx))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@OrderId", SqlDbType.Int).Value = enrollment.orderId;
                command.Parameters.Add("@PaymentId", SqlDbType.Int).Value = enrollment.PaymentId;

                command.Parameters.Add("@ExpireAt", SqlDbType.DateTime2)
                    .Value = (object?)enrollment.ExpireAt ?? DBNull.Value;

                int rowsAffected = await command.ExecuteNonQueryAsync();

                return rowsAffected > 0;

            }
        }

        public static async Task<DtoEnrollment> GetEnrollmentByUserId(int Id)
        {
            if (Id <= 0) return null;

            string query = @"SELECT Id, UserId, ProductId, EnrolledAt, ExpireAt, PaymentId
                            FROM Enrollments
                            WHERE UserId = @UserId";

            DtoEnrollment enrollment = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = Id;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int userIndex = reader.GetOrdinal("UserId");
                        int productIndex = reader.GetOrdinal("ProductId");
                        int enrolledIndex = reader.GetOrdinal("EnrolledAt");
                        int expireIndex = reader.GetOrdinal("ExpireAt");
                        int paymentIndex = reader.GetOrdinal("PaymentId");

                        enrollment = new DtoEnrollment
                        {
                            Id = reader.GetInt32(idIndex),
                            UserId = reader.GetInt32(userIndex),
                            ProductId = reader.GetInt32(productIndex),
                            EnrolledAt = reader.GetDateTime(enrolledIndex),
                            ExpireAt = reader.IsDBNull(expireIndex)
                                ? null
                                : reader.GetDateTime(expireIndex),
                            PaymentId = reader.GetInt32(paymentIndex)
                        };
                    }
                }
            }

            return enrollment;
        }

        public static async Task<bool> IsUserEnrolled(int userId, int productId)
        {
            string query = @"SELECT 1 FROM Enrollments
                        WHERE UserId = @UserId AND ProductId = @ProductId
                        AND (ExpireAt IS NULL OR ExpireAt > SYSUTCDATETIME());";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;

                await connection.OpenAsync();

                object result = await command.ExecuteScalarAsync();

                return result != null;
            }
        }

        public static async Task<List<DtoEnrollment>> GetAllUserEnrollments(int UserId,int pageNumber, int pageSize)
        {            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            string query = @"SELECT Id, UserId, ProductId, EnrolledAt, ExpireAt, PaymentId
                            FROM Enrollments
                            WHERE UserId = @UserId AND (ExpireAt IS NULL OR ExpireAt > SYSUTCDATETIME())
                            ORDER BY EnrolledAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";

            List<DtoEnrollment> enrollments = new List<DtoEnrollment>();
            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {

                    int idIndex = reader.GetOrdinal("Id");
                    int userIndex = reader.GetOrdinal("UserId");
                    int productIndex = reader.GetOrdinal("ProductId");
                    int enrolledIndex = reader.GetOrdinal("EnrolledAt");
                    int expireIndex = reader.GetOrdinal("ExpireAt");
                    int paymentIndex = reader.GetOrdinal("PaymentId");

                    

                    while (await reader.ReadAsync())
                    {
                        enrollments.Add( new DtoEnrollment
                        {
                            Id = reader.GetInt32(idIndex),
                            UserId = reader.GetInt32(userIndex),
                            ProductId = reader.GetInt32(productIndex),
                            EnrolledAt = reader.GetDateTime(enrolledIndex),
                            ExpireAt = reader.IsDBNull(expireIndex)
                            ? null
                            : reader.GetDateTime(expireIndex),
                            PaymentId = reader.GetInt32(paymentIndex)
                        });
                    }
                }
            }

            return enrollments;
        }

        // ============================================================
        // L9: enrollments joined with Products for display, plus the
        // deep-link ids (Courses.Id / Lessons.Id) the frontend needs —
        // progress endpoints take Courses.Id while enrollment carries
        // only Products.Id.
        // ============================================================
        public static async Task<List<EnrollmentViewModel>> GetUserEnrollmentViewModels(
            int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            string query = @"
                            SELECT
                                E.Id,
                                E.ProductId,
                                P.Name          AS ProductName,
                                P.ProductType   AS ProductTypeId,
                                P.ThumbnailUrl,
                                P.Summary,
                                E.EnrolledAt,
                                E.ExpireAt,
                                C.Id            AS CourseId,
                                L.Id            AS LessonId
                            FROM Enrollments E
                            INNER JOIN Products P ON E.ProductId = P.Id
                            LEFT  JOIN Courses  C ON C.ProductId = E.ProductId AND C.IsDeleted = 0
                            LEFT  JOIN Lessons  L ON L.ProductId = E.ProductId AND L.IsDeleted = 0
                            WHERE E.UserId = @UserId
                              AND (E.ExpireAt IS NULL OR E.ExpireAt > SYSUTCDATETIME())
                            ORDER BY E.EnrolledAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";

            var result = new List<EnrollmentViewModel>();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                command.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int productNameIndex = reader.GetOrdinal("ProductName");
                    int productTypeIdIndex = reader.GetOrdinal("ProductTypeId");
                    int thumbnailIndex = reader.GetOrdinal("ThumbnailUrl");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int enrolledAtIndex = reader.GetOrdinal("EnrolledAt");
                    int expireAtIndex = reader.GetOrdinal("ExpireAt");
                    int courseIdIndex = reader.GetOrdinal("CourseId");
                    int lessonIdIndex = reader.GetOrdinal("LessonId");

                    while (await reader.ReadAsync())
                    {
                        result.Add(new EnrollmentViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIdIndex),
                            ProductName = reader.GetString(productNameIndex),
                            ProductTypeId = reader.GetByte(productTypeIdIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbnailIndex)
                                ? null
                                : reader.GetString(thumbnailIndex),
                            Summary = reader.IsDBNull(summaryIndex)
                                ? null
                                : reader.GetString(summaryIndex),
                            EnrolledAt = reader.GetDateTime(enrolledAtIndex),
                            ExpireAt = reader.IsDBNull(expireAtIndex)
                                ? null
                                : reader.GetDateTime(expireAtIndex),
                            CourseId = reader.IsDBNull(courseIdIndex)
                                ? null
                                : reader.GetInt32(courseIdIndex),
                            LessonId = reader.IsDBNull(lessonIdIndex)
                                ? null
                                : reader.GetInt32(lessonIdIndex),
                        });
                    }
                }
            }

            return result;
        }

    }
}
