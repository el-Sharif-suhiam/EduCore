using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsEnrollmentsData
    {

        public static int AddEnrollment(DtoEnrollment enrollment)
        {
            string query = @"INSERT INTO Enrollments (UserId, ProductId, EnrolledAt, ExpireAt, PaymentId)
                            VALUES (@UserId, @ProductId, @EnrolledAt, @ExpireAt, @PaymentId);
                            SELECT SCOPE_IDENTITY();";

            int id = -1;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = enrollment.UserId;
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = enrollment.ProductId;
                command.Parameters.Add("@EnrolledAt", SqlDbType.DateTime).Value =
                    enrollment.EnrolledAt == default ? DateTime.Now : enrollment.EnrolledAt;

                command.Parameters.Add("@ExpireAt", SqlDbType.Date).Value =
                    (object?)enrollment.ExpireAt ?? DBNull.Value;

                command.Parameters.Add("@PaymentId", SqlDbType.Int).Value = enrollment.PaymentId;

                connection.Open();

                object result = command.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int newId))
                {
                    id = newId;
                }
            }

            return id;
        }

        public static DtoEnrollment GetEnrollmentByUserId(int Id)
        {
            if (Id <= 0) return null;

            string query = @"SELECT Id, UserId, ProductId, EnrolledAt, ExpireAt, PaymentId
                            FROM Enrollments
                            WHERE Id = @Id";

            DtoEnrollment enrollment = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = Id;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
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

        public static bool IsEnrollmentExists(int userId, int productId)
        {
            string query = @"SELECT 1 FROM Enrollments
                        WHERE UserId = @UserId AND ProductId = @ProductId";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;

                connection.Open();

                object result = command.ExecuteScalar();

                return result != null;
            }
        }

        public static List<DtoEnrollment> GetAllUserEnrollments(int UserId,int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            string query = @"SELECT Id, UserId, ProductId, EnrolledAt, ExpireAt, PaymentId
                            FROM Enrollments
                            WHERE UserId = @UserId AND (ExpireAt IS NULL OR ExpireAt < GETDATE())
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
                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {

                    int idIndex = reader.GetOrdinal("Id");
                    int userIndex = reader.GetOrdinal("UserId");
                    int productIndex = reader.GetOrdinal("ProductId");
                    int enrolledIndex = reader.GetOrdinal("EnrolledAt");
                    int expireIndex = reader.GetOrdinal("ExpireAt");
                    int paymentIndex = reader.GetOrdinal("PaymentId");

                    

                    while (reader.Read())
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

    }
}
