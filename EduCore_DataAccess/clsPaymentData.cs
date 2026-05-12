using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Common;
using Common.Enums;
namespace EduCore_DataAccess
{
    public class clsPaymentData
    {
        public int AddPayment(DtoPayment p)
        {
            string query = @"INSERT INTO Payments
                            (OrderId, Price, DiscountId, DiscountPrice, PaymentMethod, Status, TransactionId)
                            VALUES
                            (@OrderId, @Price, @DiscountId, @DiscountPrice, @PaymentMethod, @Status, @TransactionId);
                            SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = p.OrderId;
                cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = p.Price;

                cmd.Parameters.Add("@DiscountId", SqlDbType.SmallInt).Value =
                    (object?)p.DiscountId ?? DBNull.Value;

                cmd.Parameters.Add("@DiscountPrice", SqlDbType.Decimal).Value =
                    (object?)p.DiscountPrice ?? DBNull.Value;

                cmd.Parameters.Add("@PaymentMethod", SqlDbType.NVarChar, 50).Value =
                    (object?)p.PaymentMethod ?? DBNull.Value;

                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50).Value = p.Status.ToString();

                cmd.Parameters.Add("@TransactionId", SqlDbType.VarChar, 200).Value =
                    (object?)p.TransactionId ?? DBNull.Value;

                con.Open();

                var result = cmd.ExecuteScalar();

                return (result != null && int.TryParse(result.ToString(), out int id)) ? id : -1;
            }
        }

        // ===== GET BY ID =====
        public DtoPayment GetPaymentById(int id)
        {
            string query = @"SELECT Id, OrderId, PaidAt, Price, DiscountId, DiscountPrice,
                            PaymentMethod, Status, TransactionId, PayedPrice
                            FROM Payments
                            WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                con.Open();

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new DtoPayment
                        {
                            Id = r.GetInt32(0),
                            OrderId = r.GetInt32(1),
                            PaidAt = r.GetDateTime(2),
                            Price = r.GetDecimal(3),
                            DiscountId = r.IsDBNull(4) ? null : r.GetInt16(4),
                            DiscountPrice = r.IsDBNull(5) ? null : r.GetDecimal(5),
                            PaymentMethod = r.IsDBNull(6) ? null : r.GetString(6),
                            Status = (enStatus)Enum.Parse(typeof(enStatus), r.GetString(7)),
                            TransactionId = r.IsDBNull(8) ? null : r.GetString(8),
                            PayedPrice = r.GetDecimal(9)
                        };
                    }
                }
            }

            return null;
        }

        public List<DtoPayment> GetAllPayments(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoPayment> payments = new List<DtoPayment>();
            string query = @"SELECT Id, OrderId, PaidAt, Price, DiscountId, DiscountPrice,
                            PaymentMethod, Status, TransactionId, PayedPrice
                            FROM Payments
                            ORDER BY PaidAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";
          

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;


                sqlConnection.Open();

                using (SqlDataReader r = sqlCommand.ExecuteReader())
                {
                    while (r.Read())
                    {

                        payments.Add(new DtoPayment
                        {
                            Id = r.GetInt32(0),
                            OrderId = r.GetInt32(1),
                            PaidAt = r.GetDateTime(2),
                            Price = r.GetDecimal(3),
                            DiscountId = r.IsDBNull(4) ? null : r.GetInt16(4),
                            DiscountPrice = r.IsDBNull(5) ? null : r.GetDecimal(5),
                            PaymentMethod = r.IsDBNull(6) ? null : r.GetString(6),
                            Status = (enStatus)Enum.Parse(typeof(enStatus), r.GetString(7)),
                            TransactionId = r.IsDBNull(8) ? null : r.GetString(8),
                            PayedPrice = r.GetDecimal(9)
                        });
                    }
                }
            }

            return payments;
        }


        public List<DtoPayment> GetAllPaymentsForUser(int pageNumber, int pageSize,int UserId)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoPayment> payments = new List<DtoPayment>();
            string query = @"SELECT P.Id,O.UserId, OrderId, PaidAt, Price, DiscountId, DiscountPrice,
                            PaymentMethod, P.Status, TransactionId, PayedPrice
                            FROM Payments P
                            JOIN Orders O ON OrderId = O.Id
                            WHERE UserId = @UserId
                            ORDER BY PaidAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";


            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                sqlCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;


                sqlConnection.Open();

                using (SqlDataReader r = sqlCommand.ExecuteReader())
                {
                    while (r.Read())
                    {

                        payments.Add(new DtoPayment
                        {
                            Id = r.GetInt32(0),
                            OrderId = r.GetInt32(1),
                            PaidAt = r.GetDateTime(2),
                            Price = r.GetDecimal(3),
                            DiscountId = r.IsDBNull(4) ? null : r.GetInt16(4),
                            DiscountPrice = r.IsDBNull(5) ? null : r.GetDecimal(5),
                            PaymentMethod = r.IsDBNull(6) ? null : r.GetString(6),
                            Status = (enStatus)Enum.Parse(typeof(enStatus), r.GetString(7)),
                            TransactionId = r.IsDBNull(8) ? null : r.GetString(8),
                            PayedPrice = r.GetDecimal(9)
                        });
                    }
                }
            }

            return payments;
        }
        // ===== UPDATE STATUS =====
        public bool UpdateStatus(int id, enPaymentStatus status)
        {
            string query = @"UPDATE Payments SET Status = @Status WHERE Id = @Id";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50).Value = status.ToString();

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
        }
}
