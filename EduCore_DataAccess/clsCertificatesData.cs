using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace EduCore_DataAccess
{
    public class clsCertificatesData
    {
        /// <summary>
        /// Idempotent issuance via SP_IssueCertificate: the stored procedure
        /// verifies enrollment + full completion server-side and returns the
        /// existing certificate when one was already issued.
        /// </summary>
        public static async Task<DtoCertificate?> IssueCertificate(int userId, int courseId)
        {
            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SP_IssueCertificate", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new DtoCertificate
                        {
                            CertificateCode = reader.GetString(reader.GetOrdinal("CertificateCode")),
                            IssuedAt = reader.GetDateTime(reader.GetOrdinal("IssuedAt"))
                        };
                    }
                }
            }

            return null;
        }

        public static async Task<DtoCertificate?> GetByCode(string certificateCode)
        {
            const string query = @"
                            SELECT C.Id,
                                   C.CertificateCode,
                                   C.UserId,
                                   C.CourseId,
                                   C.IssuedAt,
                                   U.Name AS StudentName,
                                   P.Name AS CourseName
                            FROM Certificates C
                            JOIN Users U ON U.Id = C.UserId
                            JOIN Courses CO ON CO.Id = C.CourseId
                            JOIN Products P ON P.Id = CO.ProductId
                            WHERE C.CertificateCode = @Code;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 64).Value = certificateCode;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new DtoCertificate
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            CertificateCode = reader.GetString(reader.GetOrdinal("CertificateCode")),
                            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                            CourseId = reader.GetInt32(reader.GetOrdinal("CourseId")),
                            IssuedAt = reader.GetDateTime(reader.GetOrdinal("IssuedAt")),
                            StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                            CourseTitle = reader.GetString(reader.GetOrdinal("CourseName"))
                        };
                    }
                }
            }

            return null;
        }
    }
}
