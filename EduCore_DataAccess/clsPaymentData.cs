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
        public static async Task<int> AddPayment(DtoPayment p)
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

                await con.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                return (result != null && int.TryParse(result.ToString(), out int id)) ? id : -1;
            }
        }

        public static async Task<DtoPayment> GetPaymentById(int id)
        {
            string query = @"SELECT Id, OrderId, PaidAt, Price, DiscountId, DiscountPrice,
                            PaymentMethod, Status, TransactionId, PayedPrice
                            FROM Payments
                            WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int orderIdIndex = reader.GetOrdinal("OrderId");
                        int paidAtIndex = reader.GetOrdinal("PaidAt");
                        int priceIndex = reader.GetOrdinal("Price");
                        int discountIdIndex = reader.GetOrdinal("DiscountId");
                        int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                        int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                        int statusIndex = reader.GetOrdinal("Status");
                        int transactionIdIndex = reader.GetOrdinal("TransactionId");
                        int payedPriceIndex = reader.GetOrdinal("PayedPrice");
                        return new DtoPayment
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),
                            PaidAt = reader.GetDateTime(paidAtIndex),
                            Price = reader.GetDecimal(priceIndex),
                            DiscountId = reader.IsDBNull(discountIdIndex) ? null : reader.GetInt16(discountIdIndex),
                            DiscountPrice = reader.IsDBNull(discountPriceIndex) ? null : reader.GetDecimal(discountPriceIndex),
                            PaymentMethod = reader.IsDBNull(paymentMethodIndex) ? null : reader.GetString(paymentMethodIndex),
                            Status = (enPaymentStatus)Enum.Parse(typeof(enPaymentStatus), reader.GetString(statusIndex)),
                            TransactionId = reader.IsDBNull(transactionIdIndex) ? null : reader.GetString(transactionIdIndex),
                            PayedPrice = reader.GetDecimal(payedPriceIndex)
                        };
                    }
                }
            }

            return null;
        }

        public static async Task<List<DtoPayment>> GetAllPayments(int pageNumber, int pageSize)
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


                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int orderIdIndex = reader.GetOrdinal("OrderId");
                    int paidAtIndex = reader.GetOrdinal("PaidAt");
                    int priceIndex = reader.GetOrdinal("Price");
                    int discountIdIndex = reader.GetOrdinal("DiscountId");
                    int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                    int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                    int statusIndex = reader.GetOrdinal("Status");
                    int transactionIdIndex = reader.GetOrdinal("TransactionId");
                    int payedPriceIndex = reader.GetOrdinal("PayedPrice");

                    while (await reader.ReadAsync())
                    {

                        payments.Add(new DtoPayment
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),
                            PaidAt = reader.GetDateTime(paidAtIndex),
                            Price = reader.GetDecimal(priceIndex),
                            DiscountId = reader.IsDBNull(discountIdIndex) ? null : reader.GetInt16(discountIdIndex),
                            DiscountPrice = reader.IsDBNull(discountPriceIndex) ? null : reader.GetDecimal(discountPriceIndex),
                            PaymentMethod = reader.IsDBNull(paymentMethodIndex) ? null : reader.GetString(paymentMethodIndex),
                            Status = (enPaymentStatus)Enum.Parse(typeof(enPaymentStatus), reader.GetString(statusIndex)),
                            TransactionId = reader.IsDBNull(transactionIdIndex) ? null : reader.GetString(transactionIdIndex),
                            PayedPrice = reader.GetDecimal(payedPriceIndex)
                        });
                    }
                }
            }

            return payments;
        }


        public static async Task<List<DtoPayment>> GetAllPaymentsForUser(int pageNumber, int pageSize,int UserId)
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


                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int orderIdIndex = reader.GetOrdinal("OrderId");
                    int paidAtIndex = reader.GetOrdinal("PaidAt");
                    int priceIndex = reader.GetOrdinal("Price");
                    int discountIdIndex = reader.GetOrdinal("DiscountId");
                    int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                    int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                    int statusIndex = reader.GetOrdinal("Status");
                    int transactionIdIndex = reader.GetOrdinal("TransactionId");
                    int payedPriceIndex = reader.GetOrdinal("PayedPrice");

                    while (await reader.ReadAsync())
                    {

                        payments.Add(new DtoPayment
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),
                            PaidAt = reader.GetDateTime(paidAtIndex),
                            Price = reader.GetDecimal(priceIndex),
                            DiscountId = reader.IsDBNull(discountIdIndex) ? null : reader.GetInt16(discountIdIndex),
                            DiscountPrice = reader.IsDBNull(discountPriceIndex) ? null : reader.GetDecimal(discountPriceIndex),
                            PaymentMethod = reader.IsDBNull(paymentMethodIndex) ? null : reader.GetString(paymentMethodIndex),
                            Status = (enPaymentStatus)Enum.Parse(typeof(enPaymentStatus), reader.GetString(statusIndex)),
                            TransactionId = reader.IsDBNull(transactionIdIndex) ? null : reader.GetString(transactionIdIndex),
                            PayedPrice = reader.GetDecimal(payedPriceIndex)
                        });
                    }
                }
            }

            return payments;
        }
        public static async Task<bool> UpdateStatus(int id, enPaymentStatus status)
        {
            string query = @"UPDATE Payments SET Status = @Status WHERE Id = @Id";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50).Value = status.ToString();

                await con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
        }
        }
}
