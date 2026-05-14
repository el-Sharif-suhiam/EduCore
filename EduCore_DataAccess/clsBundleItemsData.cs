using Common.Dtos;
using Common.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using static Common.ViewModels.BundleItemsViewModel;

namespace EduCore_DataAccess
{
    public class clsBundleItemsData
    {
        public static async Task<bool> AddItemToBundle(DtoBundleItem bundleItem)
        {
            string query = @"INSERT INTO BundlesItems 
                        (BundleId,CourseId)
                        VALUES 
                        (@BundleId,@CourseId);";
            int rows = 0;
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@BundleId", SqlDbType.SmallInt).Value = bundleItem.BundleId;
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = bundleItem.CourseId;
                await connection.OpenAsync();

                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        public static async Task<bool> DeleteItemFromBundle(DtoBundleItem bundleItem)
        {
            string query = @"DELETE FROM BundlesItems 
                         WHERE CourseId = @CourseId AND BundleId = @BundleId;";
            int rows = 0;
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = bundleItem.CourseId;
                command.Parameters.Add("@BundleId", SqlDbType.SmallInt).Value = bundleItem.BundleId;
                await connection.OpenAsync();

                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        public static async Task<BundleItemsViewModel> GetBundleWithCourses(short bundleId)
        {
            const string query = @"
                                    SELECT 
                                        B.Id            AS Id,
                                        BP.Name         AS BundleName,
                                        C.Id            AS CourseId,
                                        CP.Name         AS CourseName,
                                        CP.Summary      AS CourseSummary,
                                        CP.ThumbnailUrl AS CourseThumbnailUrl
                                    FROM Bundles B
                                    JOIN Products BP        ON BP.Id = B.ProductId
                                    JOIN BundlesItems BI    ON BI.BundleId = B.Id
                                    JOIN Courses C          ON C.Id = BI.CourseId
                                    JOIN Products CP        ON CP.Id = C.ProductId
                                    WHERE BP.IsPublished = 1 AND B.Id = @Id";

            BundleItemsViewModel bundle = new BundleItemsViewModel();

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.SmallInt).Value = bundleId;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int bundleNameIndex = reader.GetOrdinal("BundleName");
                    int courseIdIndex = reader.GetOrdinal("CourseId");
                    int courseNameIndex = reader.GetOrdinal("CourseName");
                    int summaryIndex = reader.GetOrdinal("CourseSummary");
                    int thumbnailIndex = reader.GetOrdinal("CourseThumbnailUrl");

                    while (await reader.ReadAsync())
                    {
                        if (bundle is null)
                        {
                            bundle = new BundleItemsViewModel
                            {
                                Id = reader.GetInt16(idIndex),
                                BundleName = reader.GetString(bundleNameIndex),
                                Courses = new List<CourseItemVM>()
                            };
                        }

                        bundle.Courses.Add(new CourseItemVM
                        {
                            CourseId = reader.GetInt32(courseIdIndex),
                            Name = reader.GetString(courseNameIndex),
                            Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbnailIndex) ? null : reader.GetString(thumbnailIndex)
                        });
                    }
                }
            }

            return bundle;
        }
    }
}
