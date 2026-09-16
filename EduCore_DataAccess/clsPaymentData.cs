using Common.Dtos;
using Common.ViewModels;
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
        public static async Task<DtoPaymentInitRespone> CreatePaymentAsync(int orderId,string idempotencyKey,string paymentMethod,short? discountId = null)
        {
            DtoPaymentInitRespone paymentInitRespone = null;
            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SP_CreateNewPayment", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                cmd.Parameters.Add("@IdempotencyKey", SqlDbType.VarChar, 255).Value = idempotencyKey;
                cmd.Parameters.Add("@paymentMethod", SqlDbType.NVarChar,60).Value = paymentMethod;
                cmd.Parameters.Add("@DiscountId", SqlDbType.SmallInt).Value =
                    (object?)discountId ?? DBNull.Value;


                await con.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int paymentId = reader.GetInt32(reader.GetOrdinal("PaymentId"));
                        decimal finalPrice = reader.GetDecimal(reader.GetOrdinal("FinalPrice"));
                        paymentInitRespone = new DtoPaymentInitRespone
                        {
                            Id = paymentId,
                            FinalPrice = finalPrice,
                        };
                        
                    }
                }
            }

            return paymentInitRespone;
        }

        public static async Task<bool> IsIdempotencyKeyUsedAsync(string key)
        {
            string query = @"
        SELECT 1
        FROM Payments
        WHERE IdempotencyKey = @Key;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Key", SqlDbType.VarChar, 255).Value = key;

                await con.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                return result != null;
            }
        }
        public static async Task<DtoPayment?> GetPaymentByIdAsync(int id)
        {
            string query = @"
                            SELECT 
                                Id,
                                OrderId,
                                CreatedAt,
                                PaidAt,
                                Price,
                                DiscountId,
                                DiscountPrice,
                                PaymentMethod,
                                Status,
                                TransactionId,
                                IdempotencyKey,
                                FinalPrice
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
                        int createdAtIndex = reader.GetOrdinal("CreatedAt");
                        int paidAtIndex = reader.GetOrdinal("PaidAt");
                        int priceIndex = reader.GetOrdinal("Price");
                        int discountIdIndex = reader.GetOrdinal("DiscountId");
                        int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                        int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                        int statusIndex = reader.GetOrdinal("Status");
                        int transactionIdIndex = reader.GetOrdinal("TransactionId");
                        int idempotencyKeyIndex = reader.GetOrdinal("IdempotencyKey");
                        int finalPriceIndex = reader.GetOrdinal("FinalPrice");
                        return new DtoPayment
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),

                            CreatedAt = reader.GetDateTime(createdAtIndex),

                            PaidAt = reader.IsDBNull(paidAtIndex)
                                ? null
                                : reader.GetDateTime(paidAtIndex),


                            Price = reader.GetDecimal(priceIndex),

                            DiscountId = reader.IsDBNull(discountIdIndex)
                                ? null
                                : reader.GetInt16(discountIdIndex),

                            DiscountPrice = reader.IsDBNull(discountPriceIndex)
                                ? null
                                : reader.GetDecimal(discountPriceIndex),

                            PaymentMethod = reader.IsDBNull(paymentMethodIndex)
                                ? null
                                : reader.GetString(paymentMethodIndex),

                            Status = (enPaymentStatus)Enum.Parse(
                                typeof(enPaymentStatus),
                                reader.GetString(statusIndex)
                            ),

                            TransactionId = reader.IsDBNull(transactionIdIndex)
                                ? null
                                : reader.GetString(transactionIdIndex),

                            IdempotencyKey = reader.GetString(idempotencyKeyIndex),

                            FinalPrice = reader.GetDecimal(finalPriceIndex)
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

            List<DtoPayment> payments = new();

            string query = @"
                            SELECT 
                                Id,
                                OrderId,
                                CreatedAt,
                                PaidAt,
                                Price,
                                DiscountId,
                                DiscountPrice,
                                PaymentMethod,
                                Status,
                                TransactionId,
                                IdempotencyKey,
                                FinalPrice
                            FROM Payments
                            ORDER BY CreatedAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;

                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int orderIdIndex = reader.GetOrdinal("OrderId");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int paidAtIndex = reader.GetOrdinal("PaidAt");
                    int priceIndex = reader.GetOrdinal("Price");
                    int discountIdIndex = reader.GetOrdinal("DiscountId");
                    int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                    int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                    int statusIndex = reader.GetOrdinal("Status");
                    int transactionIdIndex = reader.GetOrdinal("TransactionId");
                    int idempotencyKeyIndex = reader.GetOrdinal("IdempotencyKey");
                    int finalPriceIndex = reader.GetOrdinal("FinalPrice");

                    while (await reader.ReadAsync())
                    {
                        payments.Add(new DtoPayment
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),
                            CreatedAt = reader.GetDateTime(createdAtIndex),

                            PaidAt = reader.IsDBNull(paidAtIndex)
                                ? null
                                : reader.GetDateTime(paidAtIndex),

                            Price = reader.GetDecimal(priceIndex),

                            DiscountId = reader.IsDBNull(discountIdIndex)
                                ? null
                                : reader.GetInt16(discountIdIndex),

                            DiscountPrice = reader.IsDBNull(discountPriceIndex)
                                ? null
                                : reader.GetDecimal(discountPriceIndex),

                            PaymentMethod = reader.IsDBNull(paymentMethodIndex)
                                ? null
                                : reader.GetString(paymentMethodIndex),

                            Status = Enum.Parse<enPaymentStatus>(
                                reader.GetString(statusIndex)
                            ),

                            TransactionId = reader.IsDBNull(transactionIdIndex)
                                ? null
                                : reader.GetString(transactionIdIndex),

                            IdempotencyKey = reader.GetString(idempotencyKeyIndex),

                            FinalPrice = reader.GetDecimal(finalPriceIndex)
                        });
                    }
                }
            }

            return payments;
        }

        /// <summary>Admin feed of all payments, newest first, joined with the buyer.</summary>
        public static async Task<List<PaymentAdminViewModel>> GetAllPaymentsView(int pageNumber, int pageSize, string searchText = "")
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;

            var payments = new List<PaymentAdminViewModel>();

            string query = @"SELECT
                                P.Id,
                                P.OrderId,
                                O.UserId,
                                U.Name      AS UserName,
                                U.Email     AS UserEmail,
                                P.CreatedAt,
                                P.PaidAt,
                                P.Price,
                                P.DiscountId,
                                P.DiscountPrice,
                                P.PaymentMethod,
                                P.Status,
                                P.TransactionId,
                                P.FinalPrice
                             FROM Payments P
                             JOIN Orders O ON P.OrderId = O.Id
                             JOIN Users U ON O.UserId = U.Id
                             WHERE (@SearchText IS NULL OR U.Name LIKE @SearchText OR U.Email LIKE @SearchText)
                             ORDER BY P.CreatedAt DESC
                             OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                             FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar)
                    .Value = String.IsNullOrWhiteSpace(searchText) ? DBNull.Value : $"%{searchText}%";

                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int orderIdIndex = reader.GetOrdinal("OrderId");
                    int userIdIndex = reader.GetOrdinal("UserId");
                    int userNameIndex = reader.GetOrdinal("UserName");
                    int userEmailIndex = reader.GetOrdinal("UserEmail");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int paidAtIndex = reader.GetOrdinal("PaidAt");
                    int priceIndex = reader.GetOrdinal("Price");
                    int discountIdIndex = reader.GetOrdinal("DiscountId");
                    int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                    int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                    int statusIndex = reader.GetOrdinal("Status");
                    int transactionIdIndex = reader.GetOrdinal("TransactionId");
                    int finalPriceIndex = reader.GetOrdinal("FinalPrice");

                    while (await reader.ReadAsync())
                    {
                        payments.Add(new PaymentAdminViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),
                            UserId = reader.GetInt32(userIdIndex),
                            UserName = reader.GetString(userNameIndex),
                            UserEmail = reader.GetString(userEmailIndex),
                            CreatedAt = reader.GetDateTime(createdAtIndex),
                            PaidAt = reader.IsDBNull(paidAtIndex) ? null : reader.GetDateTime(paidAtIndex),
                            Price = reader.GetDecimal(priceIndex),
                            DiscountId = reader.IsDBNull(discountIdIndex) ? null : reader.GetInt16(discountIdIndex),
                            DiscountPrice = reader.IsDBNull(discountPriceIndex) ? null : reader.GetDecimal(discountPriceIndex),
                            PaymentMethod = reader.IsDBNull(paymentMethodIndex) ? null : reader.GetString(paymentMethodIndex),
                            Status = Enum.Parse<enPaymentStatus>(reader.GetString(statusIndex)),
                            TransactionId = reader.IsDBNull(transactionIdIndex) ? null : reader.GetString(transactionIdIndex),
                            FinalPrice = reader.GetDecimal(finalPriceIndex)
                        });
                    }
                }
            }

            return payments;
        }

        public static async Task<List<DtoPayment>> GetAllPaymentsForUser(int pageNumber, int pageSize, int userId)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoPayment> payments = new();

            string query = @"
                            SELECT 
                                P.Id,
                                P.OrderId,
                                P.CreatedAt,
                                P.PaidAt,
                                P.Price,
                                P.DiscountId,
                                P.DiscountPrice,
                                P.PaymentMethod,
                                P.Status,
                                P.TransactionId,
                                P.IdempotencyKey,
                                P.FinalPrice
                            FROM Payments P
                            JOIN Orders O ON P.OrderId = O.Id
                            WHERE O.UserId = @UserId
                            ORDER BY P.CreatedAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int orderIdIndex = reader.GetOrdinal("OrderId");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int paidAtIndex = reader.GetOrdinal("PaidAt");
                    int priceIndex = reader.GetOrdinal("Price");
                    int discountIdIndex = reader.GetOrdinal("DiscountId");
                    int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                    int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                    int statusIndex = reader.GetOrdinal("Status");
                    int transactionIdIndex = reader.GetOrdinal("TransactionId");
                    int idempotencyKeyIndex = reader.GetOrdinal("IdempotencyKey");
                    int finalPriceIndex = reader.GetOrdinal("FinalPrice");

                    while (await reader.ReadAsync())
                    {
                        payments.Add(new DtoPayment
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),
                            CreatedAt = reader.GetDateTime(createdAtIndex),

                            PaidAt = reader.IsDBNull(paidAtIndex)
                                ? null
                                : reader.GetDateTime(paidAtIndex),

                            Price = reader.GetDecimal(priceIndex),

                            DiscountId = reader.IsDBNull(discountIdIndex)
                                ? null
                                : reader.GetInt16(discountIdIndex),

                            DiscountPrice = reader.IsDBNull(discountPriceIndex)
                                ? null
                                : reader.GetDecimal(discountPriceIndex),

                            PaymentMethod = reader.IsDBNull(paymentMethodIndex)
                                ? null
                                : reader.GetString(paymentMethodIndex),

                            Status = Enum.Parse<enPaymentStatus>(
                                reader.GetString(statusIndex)
                            ),

                            TransactionId = reader.IsDBNull(transactionIdIndex)
                                ? null
                                : reader.GetString(transactionIdIndex),

                            IdempotencyKey = reader.GetString(idempotencyKeyIndex),

                            FinalPrice = reader.GetDecimal(finalPriceIndex)
                        });
                    }
                }
            }

            return payments;
        }

        public static async Task<bool> UpdateStatusWithTransaction(int id, enPaymentStatus status,string? transactionId, DateTime? paidAt,SqlConnection conn,SqlTransaction? tx)
        {
            string query = @"UPDATE Payments
                            SET PaidAt = @PaidAt ,Status = @Status ,TransactionId = @TransactionId
                            WHERE Id = @Id";

            using (SqlCommand cmd = new SqlCommand(query, conn,tx))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50).Value = status.ToString();
                cmd.Parameters.Add("@PaidAt", SqlDbType.DateTime).Value = (object?)paidAt ?? DBNull.Value;
                cmd.Parameters.Add("@TransactionId", SqlDbType.NVarChar,200).Value = (object?)transactionId ?? DBNull.Value; 


                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
        }
       
        public static async Task<bool> UpdateStatus(int id,enPaymentStatus status, string? transactionId, DateTime? paidAt)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                return await UpdateStatusWithTransaction(id, status, transactionId, paidAt, conn,null);
            }
        }

        public static async Task<bool> HasSucceededPaymentForOrder(int orderId)
        {
            string query = @"SELECT 1
                             FROM Payments
                             WHERE OrderId = @OrderId AND Status = 'Succeeded';";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                await con.OpenAsync();

                object result = await cmd.ExecuteScalarAsync();

                return result != null;
            }
        }

        public static async Task<int> ExpirePendingPaymentsForOrder(int orderId, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Payments
                             SET Status = 'Expired'
                             WHERE OrderId = @OrderId AND Status = 'Pending';";

            using (SqlCommand cmd = new SqlCommand(query, conn, tx))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task<DtoPayment?> GetPaymentByIdempotencyKeyAsync(string key)
        {
            string query = @"
                            SELECT 
                                Id,
                                OrderId,
                                CreatedAt,
                                PaidAt,
                                Price,
                                DiscountId,
                                DiscountPrice,
                                PaymentMethod,
                                Status,
                                TransactionId,
                                IdempotencyKey,
                                FinalPrice
                            FROM Payments
                            WHERE IdempotencyKey = @Key;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Key", SqlDbType.VarChar, 255).Value = key;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int orderIdIndex = reader.GetOrdinal("OrderId");
                        int createdAtIndex = reader.GetOrdinal("CreatedAt");
                        int paidAtIndex = reader.GetOrdinal("PaidAt");
                        int priceIndex = reader.GetOrdinal("Price");
                        int discountIdIndex = reader.GetOrdinal("DiscountId");
                        int discountPriceIndex = reader.GetOrdinal("DiscountPrice");
                        int paymentMethodIndex = reader.GetOrdinal("PaymentMethod");
                        int statusIndex = reader.GetOrdinal("Status");
                        int transactionIdIndex = reader.GetOrdinal("TransactionId");
                        int idempotencyKeyIndex = reader.GetOrdinal("IdempotencyKey");
                        int finalPriceIndex = reader.GetOrdinal("FinalPrice");

                        return new DtoPayment
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),

                            CreatedAt = reader.GetDateTime(createdAtIndex),

                            PaidAt = reader.IsDBNull(paidAtIndex)
                                ? null
                                : reader.GetDateTime(paidAtIndex),

                            Price = reader.GetDecimal(priceIndex),

                            DiscountId = reader.IsDBNull(discountIdIndex)
                                ? null
                                : reader.GetInt16(discountIdIndex),

                            DiscountPrice = reader.IsDBNull(discountPriceIndex)
                                ? null
                                : reader.GetDecimal(discountPriceIndex),

                            PaymentMethod = reader.IsDBNull(paymentMethodIndex)
                                ? null
                                : reader.GetString(paymentMethodIndex),

                            Status = Enum.Parse<enPaymentStatus>(
                                reader.GetString(statusIndex)
                            ),

                            TransactionId = reader.IsDBNull(transactionIdIndex)
                                ? null
                                : reader.GetString(transactionIdIndex),

                            IdempotencyKey = reader.GetString(idempotencyKeyIndex),

                            FinalPrice = reader.GetDecimal(finalPriceIndex)
                        };
                    }
                }
            }

            return null;
        }

    }
}
