using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Common.Dtos;
namespace EduCore_DataAccess
{
    public class clsOrderItemData
    {
        public static int AddItemToOrder(DtoOrderItem item, SqlConnection conn, SqlTransaction tx)
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

                var result = cmd.ExecuteScalar();

                return (result != null && int.TryParse(result.ToString(), out int id)) ? id : -1;
            }
        }

        public static List<DtoOrderItem> GetItemsByOrderId(int orderId)
        {
            string query = @"SELECT Id, OrderId, ProductId, PriceAtPurchase
                         FROM OrderItems
                         WHERE OrderId = @OrderId;";

            List<DtoOrderItem> items = new List<DtoOrderItem>();

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int orderIdIndex = reader.GetOrdinal("OrderId");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int priceIndex = reader.GetOrdinal("PriceAtPurchase");

                    while (reader.Read())
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

        public static bool DeleteItemFromOrder(int id, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"DELETE FROM OrderItems WHERE Id = @Id;";

            using (SqlCommand cmd = new SqlCommand(query, conn,tx))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static decimal GetTotalByOrder(int orderId)
        {
            string query = @"SELECT SUM(PriceAtPurchase)
                         FROM OrderItems
                         WHERE OrderId = @OrderId;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                con.Open();

                var result = cmd.ExecuteScalar();

                return result != DBNull.Value && result != null
                    ? Convert.ToDecimal(result)
                    : 0;
            }
        }
    }
}
