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
        public static async Task<int> AddBundle(DtoBundle bundle, SqlConnection conn,SqlTransaction tx)
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



                var result = await command.ExecuteScalarAsync();

                bundleId = result != null ? Convert.ToInt32(result) : -1;

            }

            return bundleId;
        }

        public static async Task<DtoBundle> GetBundleById(int bundleId)
        {
            if (bundleId <= 0) return null;

            DtoBundle bundle = null;

            string query = @"SELECT Id, ProductId
                         FROM Bundles
                         WHERE Id = @Id;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.SmallInt).Value = bundleId;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int productIndex = reader.GetOrdinal("ProductId");

                        bundle = new DtoBundle
                        {
                            Id = reader.GetInt16(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                        };
                    }
                }
            }

            return bundle;
        }

        public static async Task<bool> UpdateBundle(DtoBundle bundle, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Bundles
                         SET ProductId = @ProductId
                         WHERE Id = @Id;";

            int rows = 0;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bundle.Id;
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = bundle.ProductId;

                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        
        public static async Task<bool> DeleteBundle(int bundleId)
        {
            string query = @"DELETE FROM Bundles
                         WHERE Id = @Id;";

            int rows = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bundleId;

                await connection.OpenAsync();
                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        public static async Task<List<DtoBundle>> GetAllBundles()
        {
            List<DtoBundle> bundles = new List<DtoBundle>();

            string query = @"SELECT Id, ProductId
                         FROM Bundles
                         ORDER BY Id;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIndex = reader.GetOrdinal("ProductId");

                    while (await reader.ReadAsync())
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

        public static async Task<List<BundleViewModel>> GetAllBundlesView(bool includeUnpublished = false, bool ownOnly = false, int instructorId = 0)
        {
            List<BundleViewModel> bundles = new List<BundleViewModel>();

            // Public catalog: published bundles only.
            // includeUnpublished=true powers the admin console (drafts + published).
            // ownOnly=true scopes the feed to bundles CREATED by instructorId so
            // an instructor can preview their own drafts but never another
            // instructor's unpublished content. The DB enforces both via
            // Products.CreatedByUser.
            // NOTE: Products/Bundles have no soft-delete column (unlike Courses/Lessons),
            // so there is no IsDeleted filter here — deleting that predicate is what
            // kept this endpoint from 500-ing on "Invalid column name 'IsDeleted'".
            string query = @"SELECT B.Id, B.ProductId, P.Name, P.CreatedAt, P.BasePrice,
                            P.ThumbnailUrl, P.Summary, P.IsPublished
                            FROM Bundles B
                            JOIN Products P ON P.Id = B.ProductId
                            WHERE (@IncludeUnpublished = 1 OR P.IsPublished = 1)
                              AND (@OwnOnly = 0 OR P.CreatedByUser = @InstructorId);";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@IncludeUnpublished", SqlDbType.Bit).Value = includeUnpublished;
                command.Parameters.Add("@OwnOnly", SqlDbType.Bit).Value = ownOnly;
                command.Parameters.Add("@InstructorId", SqlDbType.Int).Value = instructorId;
                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int nameIndex = reader.GetOrdinal("Name");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int thumbnailIndex = reader.GetOrdinal("ThumbnailUrl");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int isPublishedIndex = reader.GetOrdinal("IsPublished");
                    while (await reader.ReadAsync())
                    {
                        bundles.Add(new BundleViewModel
                        {
                            Id = reader.GetInt16(idIndex),
                            ProductId = reader.GetInt32(productIdIndex),
                            Name = reader.GetString(nameIndex),
                            CreatedAt = reader.GetDateTime(createdAtIndex),
                            BasePrice = reader.GetDecimal(basePriceIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbnailIndex) ? null : reader.GetString(thumbnailIndex),
                            Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                            IsPublished = reader.GetBoolean(isPublishedIndex),
                        });
                    }
                }
            }
            return bundles;

        }

        /// <summary>
        /// Existence check. When <paramref name="publishedOnly"/> is set the
        /// bundle must also be published — anonymous existence probes must
        /// not reveal that a draft bundle id exists.
        /// </summary>
        public static async Task<bool> BundleExists(int bundleId, bool publishedOnly = false)
        {
            const string query = @"
                            SELECT CAST(
                                CASE WHEN EXISTS (
                                    SELECT 1
                                    FROM Bundles B
                                    JOIN Products P ON P.Id = B.ProductId
                                    WHERE B.Id = @Id
                                      AND (@PublishedOnly = 0 OR P.IsPublished = 1)
                                )
                                THEN 1 ELSE 0 END
                            AS BIT)";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bundleId;
                command.Parameters.Add("@PublishedOnly", SqlDbType.Bit).Value = publishedOnly;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return (bool)result;
            }
        }
    }
}
