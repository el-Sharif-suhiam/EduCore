using Common.Dtos;
using Common.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsBundlesData
    {
        public static int AddBundle(DtoBundle bundle, SqlConnection conn,SqlTransaction tx)
        {
            string query = @"INSERT INTO Bundles 
                        (ProductId)
                        VALUES 
                        (@ProductId);
                        SELECT SCOPE_IDENTITY();";

            int bundleId = -1;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = bundle.ProductId;



                var result = command.ExecuteScalar();

                bundleId = result != null ? Convert.ToInt32(result) : -1;

            }

            return bundleId;
        }

        public static DtoBundle GetBundleById(int bundleId)
        {
            if (bundleId <= 0) return null;

            DtoBundle bundle = null;

            string query = @"SELECT Id, ProductId
                         FROM Bundles
                         WHERE Id = @Id;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bundleId;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int productIndex = reader.GetOrdinal("ProductId");

                        bundle = new DtoBundle
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                        };
                    }
                }
            }

            return bundle;
        }

        public static bool UpdateBundle(DtoBundle bundle, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Bundles
                         SET ProductId = @ProductId,
                         WHERE Id = @Id;";

            int rows = 0;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bundle.Id;
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = bundle.ProductId;

                rows = command.ExecuteNonQuery();
            }

            return rows > 0;
        }

        public static bool DeleteBundle(int bundleId)
        {
            string query = @"DELETE FROM Bundles
                         WHERE Id = @Id;";

            int rows = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bundleId;

                connection.Open();
                rows = command.ExecuteNonQuery();
            }

            return rows > 0;
        }

        public static List<DtoBundle> GetAllBundles()
        {
            List<DtoBundle> bundles = new List<DtoBundle>();

            string query = @"SELECT Id, ProductId
                         FROM Bundles
                         ORDER BY Id;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIndex = reader.GetOrdinal("ProductId");

                    while (reader.Read())
                    {
                        bundles.Add(new DtoBundle
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                        });
                    }
                }
            }

            return bundles;
        }

        public static List<BundleViewModel> GetAllBundlesView()
        {
            List<BundleViewModel> bundles = new List<BundleViewModel>();

            string query = @"SELECT B.Id,B.ProductId , P.Name, P.CreatedAt, P.BasePrice,
                            P.ThumbnailUrl, P.Summary
                            FROM Bundles B
                            JOIN Products P ON P.Id = B.ProductId
                            WHERE P.IsPublished =1;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {

                    int idIndex = reader.GetOrdinal("Id");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int nameIndex = reader.GetOrdinal("Name");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int thumbnailIndex = reader.GetOrdinal("ThumbnailUrl");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    while (reader.Read())
                    {
                        bundles.Add(new BundleViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIdIndex),
                            Name = reader.GetString(nameIndex),
                            CreatedAt = reader.GetDateTime(createdAtIndex),
                            BasePrice = reader.GetDecimal(basePriceIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbnailIndex) ? null : reader.GetString(thumbnailIndex),
                            Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                        });
                    }
                }
            }
            return bundles;

        }

        public static bool BundleExists(int bundleId)
        {
            const string query = @"SELECT CAST(
                                CASE WHEN EXISTS (
                                    SELECT 1 FROM Bundles
                                    WHERE Id = @Id AND IsDeleted = 0
                                )
                                THEN 1 ELSE 0 END
                            AS BIT)";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bundleId;

                connection.Open();
                return (bool)command.ExecuteScalar();
            }
        }
    }
}
