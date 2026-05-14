using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
namespace EduCore_DataAccess
{
    public class clsOrderItemData
    {
        public static async Task<int> AddItemToOrder(DtoOrderItem item, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"INSERT INTO OrderItems (OrderId, ProductId, PriceAtPurchase)
                            VALUES (@OrderId, @ProductId, @Price);
                            SELECT SCOPE_IDENTITY();";

            using (SqlCommand cmd = new SqlCommand(query, conn,tx))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = item.OrderId;
                cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = item.ProductId;
                cmd.Parameters.Add("@Price", SqlDbType.Decimal)
                    .Value = (object?)item.PriceAtPurchase ?? DBNull.Value;

                var result = await cmd.ExecuteScalarAsync();

                return (result != null && int.TryParse(result.ToString(), out int id)) ? id : -1;
            }
        }

        public static async Task<DtoOrderItemRespone> AddItemToOrderAsync(int orderId, int productId)
        {
            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SP_AddNewItemToOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@OrderId",SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add("@ProductId",SqlDbType.Int).Value = productId;

                await con.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int orderItemIndex = reader.GetOrdinal("OrderItemId");
                    int totalPriceIndex = reader.GetOrdinal("TotalPrice");

                    if (await reader.ReadAsync())
                    {
                        
                        return new DtoOrderItemRespone
                        {
                            Id = reader.GetInt32(orderItemIndex),
                            NewOrderTotal = reader.GetDecimal(totalPriceIndex)
                        };
                        
                    }

                }
            }

            return null;
        }

        public static async Task<decimal> RemoveItemFromAsync(int orderId, int productId)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SP_RemoveItemFromOrder", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;

                await conn.OpenAsync();


                var result = await cmd.ExecuteScalarAsync();

                return result != null && result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0m;
            }
        }


        public static async Task<List<DtoOrderItem>> GetItemsByOrderId(int orderId)
        {
            string query = @"SELECT Id, OrderId, ProductId, PriceAtPurchase
                         FROM OrderItems
                         WHERE OrderId = @OrderId;";

            List<DtoOrderItem> items = new List<DtoOrderItem>();

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int orderIdIndex = reader.GetOrdinal("OrderId");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int priceIndex = reader.GetOrdinal("PriceAtPurchase");

                    while (await reader.ReadAsync())
                    {
                        items.Add(new DtoOrderItem
                        {
                            Id = reader.GetInt32(idIndex),
                            OrderId = reader.GetInt32(orderIdIndex),
                            ProductId = reader.GetInt32(productIdIndex),
                            PriceAtPurchase = reader.IsDBNull(priceIndex) ? null : reader.GetDecimal(priceIndex)
                        });
                    }
                }
            }

            return items;
        }

        public static async Task<bool> DeleteItemFromOrder(int id, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"DELETE FROM OrderItems WHERE Id = @Id;";

            using (SqlCommand cmd = new SqlCommand(query, conn,tx))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
        }

        public static async Task<decimal> GetTotalByOrder(int orderId)
        {
            string query = @"SELECT SUM(PriceAtPurchase)
                         FROM OrderItems
                         WHERE OrderId = @OrderId;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                await con.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                return result != DBNull.Value && result != null
                    ? Convert.ToDecimal(result)
                    : 0;
            }
        }
    }
}
