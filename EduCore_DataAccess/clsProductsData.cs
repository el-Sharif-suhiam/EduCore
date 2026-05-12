using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
namespace EduCore_DataAccess
{
    public class clsProductsData
    {
        public static DtoProduct GetProductById(int id)
        {
            DtoProduct product = new DtoProduct();

            string query = @"
                        SELECT Id, ProductType, Name, CreatedAt, UpdatedAt, 
                               BasePrice, CreatedByAdmin, ThumbnailUrl,Summary, IsPublished
                        FROM Products
                        WHERE Id = @Id";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int typeIndex = reader.GetOrdinal("ProductType");
                        int nameIndex = reader.GetOrdinal("Name");
                        int createdIndex = reader.GetOrdinal("CreatedAt");
                        int updatedIndex = reader.GetOrdinal("UpdatedAt");
                        int priceIndex = reader.GetOrdinal("BasePrice");
                        int adminIndex = reader.GetOrdinal("CreatedByAdmin");
                        int thumbIndex = reader.GetOrdinal("ThumbnailUrl");
                        int summaryIndex = reader.GetOrdinal("Summary");
                        int publishedIndex = reader.GetOrdinal("IsPublished");

                        product = new DtoProduct
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductType = reader.GetByte(typeIndex),
                            Name = reader.GetString(nameIndex),
                            CreatedAt = reader.GetDateTime(createdIndex),
                            UpdatedAt = reader.GetDateTime(updatedIndex),
                            BasePrice = reader.GetDecimal(priceIndex),
                            CreatedByAdmin = reader.GetInt32(adminIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbIndex) ? null : reader.GetString(thumbIndex),
                            Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                            IsPublished = reader.GetBoolean(publishedIndex)
                        };
                    }
                }
            }

            return product;
        }

        private static List<DtoProduct> GenericGetAllProduct(string query, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            List<DtoProduct> products = new List<DtoProduct>();
            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {

                    int idIndex = reader.GetOrdinal("Id");
                    int typeIndex = reader.GetOrdinal("ProductType");
                    int nameIndex = reader.GetOrdinal("Name");
                    int createdIndex = reader.GetOrdinal("CreatedAt");
                    int updatedIndex = reader.GetOrdinal("UpdatedAt");
                    int priceIndex = reader.GetOrdinal("BasePrice");
                    int adminIndex = reader.GetOrdinal("CreatedByAdmin");
                    int thumbIndex = reader.GetOrdinal("ThumbnailUrl");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int publishedIndex = reader.GetOrdinal("IsPublished");

                    while (reader.Read())
                    {
                        products.Add(new DtoProduct
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductType = reader.GetByte(typeIndex),
                            Name = reader.GetString(nameIndex),
                            CreatedAt = reader.GetDateTime(createdIndex),
                            UpdatedAt = reader.GetDateTime(updatedIndex),
                            BasePrice = reader.GetDecimal(priceIndex),
                            CreatedByAdmin = reader.GetInt32(adminIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbIndex) ? null : reader.GetString(thumbIndex),
                            Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                            IsPublished = reader.GetBoolean(publishedIndex)
                        });
                    }
                }
            }

            return products;
        }
        static public List<DtoProduct> GetAllProducts(int pageNumber, int pageSize)
        {
            string query = @"SELECT Id, ProductType, Name, CreatedAt, UpdatedAt, 
                               BasePrice, CreatedByAdmin, ThumbnailUrl,Summary, IsPublished
                               FROM Products
                            ORDER BY CreatedAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";


            return GenericGetAllProduct(query,pageNumber,pageSize);
        }

        static public List<DtoProduct> GetAllPublishedProductsByPage(int pageNumber, int pageSize)
        {

            string query = @"SELECT Id, ProductType, Name, CreatedAt, UpdatedAt, 
                               BasePrice, CreatedByAdmin, ThumbnailUrl,Summary,IsPublished
                               FROM Products
                               WHERE IsPublished = 1
                                ORDER BY CreatedAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";

            return GenericGetAllProduct(query, pageNumber, pageSize);
        }
        static public int AddProduct(DtoProduct product, SqlConnection conn, SqlTransaction tx)
        {
            int ProductID = -1;

            string query = @"INSERT INTO Products (ProductType, Name, CreatedAt, UpdatedAt, 
                               BasePrice, CreatedByAdmin, ThumbnailUrl,Summary, IsPublished) 
                                VALUES (@ProductType,@Name,GETDATE(), Null,@BasePrice, @CreatedByAdmin,@ThumbnailUrl,@Summary, @IsPublished)
                                SELECT SCOPE_IDENTITY();";


            using (SqlCommand sqlCommand = new SqlCommand(query, conn,tx))
            {
                sqlCommand.Parameters.Add("@ProductType", SqlDbType.TinyInt).Value = product.Name;
                sqlCommand.Parameters.Add("@Name", SqlDbType.NVarChar).Value = product.Name;
                sqlCommand.Parameters.Add("@BasePrice", SqlDbType.Decimal).Value = product.BasePrice;
                sqlCommand.Parameters.Add("@CreatedByAdmin", SqlDbType.Int).Value = product.CreatedByAdmin;
                sqlCommand.Parameters.Add("@ThumbnailUrl", SqlDbType.NVarChar,500).Value = product.ThumbnailUrl;
                sqlCommand.Parameters.Add("@Summary", SqlDbType.NVarChar,300).Value = product.Summary;
                sqlCommand.Parameters.Add("@IsPublished", SqlDbType.Bit).Value = product.IsPublished;

                object result = sqlCommand.ExecuteScalar();

                ProductID = result != null ? Convert.ToInt32(result) : -1;


            }

            return ProductID;
        }

        public static int AddProduct(DtoProduct product)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                conn.Open();
                return AddProduct(product, conn, null);
            }
        }
        public static bool UpdateProduct(DtoProduct product,SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Products
                             SET Name = @Name,
                                 ProductType = @Type,
                                 BasePrice = @Price,
                                 ThumbnailUrl = @Thumbnail,
                                 Summary = @Summary,
                                 UpdatedAt = GETDATE()
                             WHERE Id = @Id;";

            int rowsAffected = 0;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = product.Id;
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 200).Value = product.Name;
                command.Parameters.Add("@Type", SqlDbType.TinyInt).Value = product.ProductType;
                command.Parameters.Add("@Price", SqlDbType.Decimal).Value = product.BasePrice;
                command.Parameters.Add("@Thumbnail", SqlDbType.NVarChar, 500)
                    .Value = (object?)product.ThumbnailUrl ?? DBNull.Value;
                command.Parameters.Add("@Summary", SqlDbType.NVarChar,300).Value = (object?)product.Summary ?? DBNull.Value;

                rowsAffected = command.ExecuteNonQuery();
            }

            return rowsAffected > 0;
        }

        public static bool UpdateProduct(DtoProduct product)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                conn.Open();
                return UpdateProduct(product, conn, null);
            }
        }
        public static bool PublishProduct(int id)
        {
            string query = @"UPDATE Products
                            SET IsPublished = 1
                            WHERE Id = @Id;";

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            return rowsAffected > 0;
        }

        public static bool UnPublishProduct(int id)
        {
            string query = @"UPDATE Products
                            SET IsPublished = 0
                            WHERE Id = @Id;";

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            return rowsAffected > 0;
        }

    }
}
