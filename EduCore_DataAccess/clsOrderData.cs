using Common.Dtos;
using Common.Enums;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection.PortableExecutable;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsOrderData
    {
        public static int CreateOrder(DtoOrder order, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"INSERT INTO Orders (UserId, TotalPrice, Status)
                             VALUES (@UserId, @TotalPrice, @Status);
                             SELECT SCOPE_IDENTITY();";

            using (SqlCommand cmd = new SqlCommand(query, conn,tx))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = order.UserId;
                cmd.Parameters.Add("@TotalPrice", SqlDbType.Decimal)
                    .Value = (object?)order.TotalPrice ?? DBNull.Value;

                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50)
                    .Value = order.Status.ToString();


                var result = cmd.ExecuteScalar();

                return (result != null && int.TryParse(result.ToString(), out int id)) ? id : -1;
            }
        }

        public static DtoOrder GetOrderById(int orderId)
        {
            string query = @"SELECT Id, UserId, TotalPrice, Status, CreatedAt
                         FROM Orders
                         WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = orderId;

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int userIndex = reader.GetOrdinal("UserId");
                    int totalPriceIndex = reader.GetOrdinal("TotalPrice");
                    int statusIndex = reader.GetOrdinal("Status");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");

                    if (reader.Read())
                    {
                        return new DtoOrder
                        {
                            Id = reader.GetInt32(idIndex),
                            UserId = reader.GetInt32(userIndex),
                            TotalPrice = reader.IsDBNull(totalPriceIndex) ? null : reader.GetDecimal(totalPriceIndex),
                            Status = (enOrderStatus)Enum.Parse(typeof(enOrderStatus), reader.GetString(statusIndex)),
                            CreatedAt = reader.GetDateTime(createdAtIndex)
                        };
                    }
                }
            }

            return null;
        }

        public static bool UpdateStatus(int orderId, string status)
        {
            string query = @"UPDATE Orders
                         SET Status = @Status
                         WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50).Value = status.ToString();

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool UpdateTotal(int orderId, decimal total,SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Orders
                         SET TotalPrice = @Total
                         WHERE Id = @Id;";

            using (SqlCommand cmd = new SqlCommand(query, conn,tx))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add("@Total", SqlDbType.Decimal).Value = total;

                return cmd.ExecuteNonQuery() > 0;
            }
        }


        public static DtoOrder GetPendingOrderForUser(int UserId)
        {
            string query = @"SELECT Id, UserId, TotalPrice, Status, CreatedAt
                         FROM Orders
                         WHERE UserId = @UserId AND Status = 'Pending';";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int userIndex = reader.GetOrdinal("UserId");
                    int totalPriceIndex = reader.GetOrdinal("TotalPrice");
                    int statusIndex = reader.GetOrdinal("Status");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    if (reader.Read())
                    {
                        return new DtoOrder
                        {
                            Id = reader.GetInt32(idIndex),
                            UserId = reader.GetInt32(userIndex),
                            TotalPrice = reader.IsDBNull(totalPriceIndex) ? null : reader.GetDecimal(totalPriceIndex),
                            Status = (enOrderStatus)Enum.Parse(typeof(enOrderStatus), reader.GetString(statusIndex)),
                            CreatedAt = reader.GetDateTime(createdAtIndex)
                        };
                    }
                }
            }

            return null;
        }


    }
}
