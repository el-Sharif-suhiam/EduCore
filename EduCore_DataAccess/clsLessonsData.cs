using Common.Dtos;
using Common.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsLessonsData
    {
        public static async Task<int> AddLesson(DtoLessons lesson, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"INSERT INTO Lessons 
                        (ProductId, Title, VideoUrl, BodyText, InstructorId ,CourseId)
                        VALUES 
                        (@ProductId, @Title, @VideoUrl, @BodyText, @InstructorId,@CourseId);
                        SELECT SCOPE_IDENTITY();";

            int lessonId = -1;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = lesson.ProductId;
                command.Parameters.Add("@Title", SqlDbType.NVarChar, 150).Value = lesson.Title;

                command.Parameters.Add("@VideoUrl", SqlDbType.VarChar, 500).Value = (object?)lesson.VideoUrl ?? DBNull.Value;
                command.Parameters.Add("@BodyText", SqlDbType.NVarChar, -1).Value = (object?)lesson.BodyText ?? DBNull.Value;

                command.Parameters.Add("@InstructorId", SqlDbType.Int).Value = lesson.InstructorId;
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = (object?)lesson.CourseId ?? DBNull.Value;

                var result = await command.ExecuteScalarAsync();

                lessonId = result != null ? Convert.ToInt32(result) : -1;

            }

            return lessonId;
        }


        private static async Task<DtoLessons> GetLessonIdInternal(int lessonId, bool IncludeDeleted = false)
        {
            if (lessonId <= 0) return null;

            DtoLessons lesson = null;

            string query = @"SELECT Id, ProductId, Title, VideoUrl, BodyText, 
                                IsDeleted, DeletedAt, DeletedById, InstructorId
                         FROM Lessons
                         WHERE Id = @Id 
                         AND (@IncludeDeleted = 1 OR IsDeleted = 0) AND CourseId IS NULL;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = lessonId;
                command.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int productIndex = reader.GetOrdinal("ProductId");
                        int titleIndex = reader.GetOrdinal("Title");
                        int videoIndex = reader.GetOrdinal("VideoUrl");
                        int bodyIndex = reader.GetOrdinal("BodyText");
                        int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                        int deletedAtIndex = reader.GetOrdinal("DeletedAt");
                        int deletedByIndex = reader.GetOrdinal("DeletedById");
                        int instructorIndex = reader.GetOrdinal("InstructorId");

                        lesson = new DtoLessons
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            Title = reader.GetString(titleIndex),
                            VideoUrl = reader.IsDBNull(videoIndex) ? null : reader.GetString(videoIndex),
                            BodyText = reader.GetString(bodyIndex),
                            IsDeleted = reader.GetBoolean(isDeletedIndex),
                            DeletedAt = reader.IsDBNull(deletedAtIndex) ? null : reader.GetDateTime(deletedAtIndex),
                            DeletedById = reader.IsDBNull(deletedByIndex) ? null : reader.GetInt32(deletedByIndex),
                            InstructorId = reader.GetInt32(instructorIndex)
                        };
                    }
                }
            }

            return lesson;
        }

        public static async Task<DtoLessons> GetLessonById(int lessonId)
            => await GetLessonIdInternal(lessonId);

        public static async Task<DtoLessons> GetLessonByIdIncludeDeleted(int lessonId)
            => await GetLessonIdInternal(lessonId, true);


        public static async Task<DtoLessons> GetLessonByProductId(int productId)
        {
            if (productId <= 0) return null;

            DtoLessons lesson = null;

            string query = @"SELECT Id, ProductId, Title, VideoUrl, BodyText, 
                                IsDeleted, DeletedAt, DeletedById, InstructorId
                         FROM Lessons
                         WHERE ProductId = @ProductId AND CourseId IS NULL;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int productIndex = reader.GetOrdinal("ProductId");
                        int titleIndex = reader.GetOrdinal("Title");
                        int videoIndex = reader.GetOrdinal("VideoUrl");
                        int bodyIndex = reader.GetOrdinal("BodyText");
                        int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                        int deletedAtIndex = reader.GetOrdinal("DeletedAt");
                        int deletedByIndex = reader.GetOrdinal("DeletedById");
                        int instructorIndex = reader.GetOrdinal("InstructorId");

                        lesson = new DtoLessons
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            Title = reader.GetString(titleIndex),
                            VideoUrl = reader.IsDBNull(videoIndex) ? null : reader.GetString(videoIndex),
                            BodyText = reader.GetString(bodyIndex),
                            IsDeleted = reader.GetBoolean(isDeletedIndex),
                            DeletedAt = reader.IsDBNull(deletedAtIndex) ? null : reader.GetDateTime(deletedAtIndex),
                            DeletedById = reader.IsDBNull(deletedByIndex) ? null : reader.GetInt32(deletedByIndex),
                            InstructorId = reader.GetInt32(instructorIndex)
                        };
                    }
                }
            }

            return lesson;
        }


        public static async Task<DtoLessons> GetLessonWithCourseInternal(int lessonId, int courseId ,bool IncludeDeleted = false)
        {
            if (lessonId <= 0 || courseId <= 0) return null;

            DtoLessons lesson = null;

            string query = @"SELECT Id, ProductId, Title, VideoUrl, BodyText, 
                                IsDeleted, DeletedAt, DeletedById, InstructorId, CourseId
                         FROM Lessons
                         WHERE Id = @Id 
                         AND (@IncludeDeleted = 1 OR IsDeleted = 0) AND CourseId = @CourseId;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = lessonId;
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;
                command.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int productIndex = reader.GetOrdinal("ProductId");
                        int titleIndex = reader.GetOrdinal("Title");
                        int videoIndex = reader.GetOrdinal("VideoUrl");
                        int bodyIndex = reader.GetOrdinal("BodyText");
                        int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                        int deletedAtIndex = reader.GetOrdinal("DeletedAt");
                        int deletedByIndex = reader.GetOrdinal("DeletedById");
                        int instructorIndex = reader.GetOrdinal("InstructorId");
                        int coursIdIndex = reader.GetOrdinal("CourseId");

                        lesson = new DtoLessons
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            Title = reader.GetString(titleIndex),
                            VideoUrl = reader.IsDBNull(videoIndex) ? null : reader.GetString(videoIndex),
                            BodyText = reader.GetString(bodyIndex),
                            IsDeleted = reader.GetBoolean(isDeletedIndex),
                            DeletedAt = reader.IsDBNull(deletedAtIndex) ? null : reader.GetDateTime(deletedAtIndex),
                            DeletedById = reader.IsDBNull(deletedByIndex) ? null : reader.GetInt32(deletedByIndex),
                            InstructorId = reader.GetInt32(instructorIndex),
                            CourseId = reader.GetInt32(coursIdIndex),
                        };
                    }
                }
            }

            return lesson;
        }


        public static async Task<DtoLessons> GetLessonWithCourseById(int lessonId,int courseId)
            => await GetLessonWithCourseInternal(lessonId,courseId);

        public static async Task<DtoLessons> GetLessonWithCourseByIdIncludeDeleted(int lessonId,int courseId)
            => await GetLessonWithCourseInternal(lessonId,courseId, true);

        public static async Task<bool> UpdateLesson(DtoLessons lesson, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Lessons
                         SET Title = @Title,
                             VideoUrl = @VideoUrl,
                             BodyText = @BodyText,
                             InstructorId = @InstructorId,
                             CourseId = @CourseId
                         WHERE Id = @Id;";

            int rows = 0;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = lesson.Id;
                command.Parameters.Add("@Title", SqlDbType.NVarChar, 150).Value = lesson.Title;

                command.Parameters.Add("@VideoUrl", SqlDbType.VarChar, 500)
                    .Value = (object?)lesson.VideoUrl ?? DBNull.Value;

                command.Parameters.Add("@BodyText", SqlDbType.NVarChar,-1).Value = (object?)lesson.BodyText?? DBNull.Value;

                command.Parameters.Add("@InstructorId", SqlDbType.Int).Value = lesson.InstructorId;
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = (object?)lesson.CourseId ?? DBNull.Value;


                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        private static async Task<bool> ControlDeleteLesson(int lessonId, int AdminId, bool UnDelete = false)
        {
            string query = @"UPDATE Lessons
                         SET IsDeleted = 1,
                             DeletedAt = GETDATE(),
                             DeletedById = @AdminId
                         WHERE Id = @Id;";
            if (UnDelete)
            {
                query = @"UPDATE Lessons
                         SET IsDeleted = 0,
                             DeletedAt = NULL,
                             DeletedById = NULL
                         WHERE Id = @Id;";
            }

            int rows = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = lessonId;
                command.Parameters.Add("@AdminId", SqlDbType.Int).Value = AdminId;

                await connection.OpenAsync();
                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }
        public static async Task<bool> DeleteLesson(int lessonId,int adminId)
        {
            return await ControlDeleteLesson(lessonId, adminId);
        }

        public static async Task<bool> UnDeleteLesson(int lessonId, int adminId)
        {
            return await ControlDeleteLesson(lessonId,adminId,true);
        }

        private static async Task<List<DtoLessons>> GetAllLessonsInternal(int pageNumber, int pageSize, bool IncludeDeleted = false)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoLessons> lessons = new List<DtoLessons>();

            string query = @"SELECT Id, ProductId, Title, VideoUrl, BodyText,
                                IsDeleted, DeletedAt, DeletedById, InstructorId
                         FROM Lessons
                         WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
                         ORDER BY Id
                         OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                         FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                command.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                command.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIndex = reader.GetOrdinal("ProductId");
                    int titleIndex = reader.GetOrdinal("Title");
                    int videoIndex = reader.GetOrdinal("VideoUrl");
                    int bodyIndex = reader.GetOrdinal("BodyText");
                    int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                    int deletedAtIndex = reader.GetOrdinal("DeletedAt");
                    int deletedByIndex = reader.GetOrdinal("DeletedById");
                    int instructorIndex = reader.GetOrdinal("InstructorId");

                    while (await reader.ReadAsync())
                    {
                        lessons.Add(new DtoLessons
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            Title = reader.GetString(titleIndex),
                            VideoUrl = reader.IsDBNull(videoIndex) ? null : reader.GetString(videoIndex),
                            BodyText = reader.GetString(bodyIndex),
                            IsDeleted = reader.GetBoolean(isDeletedIndex),
                            DeletedAt = reader.IsDBNull(deletedAtIndex) ? null : reader.GetDateTime(deletedAtIndex),
                            DeletedById = reader.IsDBNull(deletedByIndex) ? null : reader.GetInt32(deletedByIndex),
                            InstructorId = reader.GetInt32(instructorIndex)
                        });
                    }
                }
            }

            return lessons;
        }

        public static async Task<List<DtoLessons>> GetAllLessons(int pageNumber, int pageSize)
            => await GetAllLessonsInternal(pageNumber, pageSize);

        public static async Task<List<DtoLessons>> GetAllLessonsIncludeDeleted(int pageNumber, int pageSize)
            => await GetAllLessonsInternal(pageNumber, pageSize, true);

        public static async Task<List<LessonsWithOutCoursesViewModel>> GetAllLessonsWithOutCourses(int pageNumber, int pageSize, string SearchText = "", bool includeUnpublished = false, bool ownOnly = false, int instructorId = 0)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<LessonsWithOutCoursesViewModel> lessons = new List<LessonsWithOutCoursesViewModel>();

            string query = @"SELECT v.Id,
                                v.ProductId,
                                v.Title,
                                v.Summary,
                                v.BasePrice,
                                v.CreatedAt,
                                v.ThumbnailUrl,
                                v.IsPublished,
                                v.InstructorId,
                                v.InstructorName
                            FROM vwLessonsWithOutCourses v
                            WHERE (@IncludeUnpublished = 1 OR v.IsPublished = 1)
                              AND (@OwnOnly = 0 OR v.InstructorId = @InstructorId)
                              AND (@SearchText IS NULL OR v.Title LIKE @SearchText OR v.InstructorName LIKE @SearchText)
	                        ORDER BY v.CreatedAt DESC
                         OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                         FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                command.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                command.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = String.IsNullOrWhiteSpace(SearchText) ? DBNull.Value : $"%{SearchText}%";
                command.Parameters.Add("@IncludeUnpublished", SqlDbType.Bit).Value = includeUnpublished;
                command.Parameters.Add("@OwnOnly", SqlDbType.Bit).Value = ownOnly;
                command.Parameters.Add("@InstructorId", SqlDbType.Int).Value = instructorId;


                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int titleIndex = reader.GetOrdinal("Title");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int thumbnailUrlIndex = reader.GetOrdinal("ThumbnailUrl");
                    int isPublishedIndex = reader.GetOrdinal("IsPublished");
                    int instructorIdIndex = reader.GetOrdinal("InstructorId");
                    int instructorNameIndex = reader.GetOrdinal("InstructorName");

                    while (await reader.ReadAsync())
                    {
                        lessons.Add(new LessonsWithOutCoursesViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIdIndex),
                            Title = reader.GetString(titleIndex),
                            Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                            BasePrice = reader.GetDecimal(basePriceIndex),
                            CreatedAt = reader.GetDateTime(createdAtIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbnailUrlIndex) ? null : reader.GetString(thumbnailUrlIndex),
                            IsPublished = reader.GetBoolean(isPublishedIndex),
                            InstructorId = reader.GetInt32(instructorIdIndex),
                            InstructorName = reader.GetString(instructorNameIndex),
                        });
                    }
                }
            }

            return lessons;
        }

        public static async Task<LessonPublicInfoViewModel?> GetLessonPublicInfo(int lessonId)
        {
            if (lessonId <= 0) return null;

            const string query = @"SELECT v.Id,
                                          v.ProductId,
                                          v.Title,
                                          v.Summary,
                                          v.BasePrice,
                                          v.CreatedAt,
                                          v.ThumbnailUrl,
                                          v.InstructorId,
                                          v.InstructorName
                                   FROM vwLessonsWithOutCourses v
                                   WHERE v.Id = @Id AND v.IsPublished = 1;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = lessonId;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                        return null;

                    int idIndex = reader.GetOrdinal("Id");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int titleIndex = reader.GetOrdinal("Title");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int thumbnailUrlIndex = reader.GetOrdinal("ThumbnailUrl");
                    int instructorIdIndex = reader.GetOrdinal("InstructorId");
                    int instructorNameIndex = reader.GetOrdinal("InstructorName");

                    return new LessonPublicInfoViewModel
                    {
                        Id = reader.GetInt32(idIndex),
                        ProductId = reader.GetInt32(productIdIndex),
                        Title = reader.GetString(titleIndex),
                        Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                        BasePrice = reader.GetDecimal(basePriceIndex),
                        CreatedAt = reader.GetDateTime(createdAtIndex),
                        ThumbnailUrl = reader.IsDBNull(thumbnailUrlIndex) ? null : reader.GetString(thumbnailUrlIndex),
                        InstructorId = reader.GetInt32(instructorIdIndex),
                        InstructorName = reader.GetString(instructorNameIndex),
                    };
                }
            }
        }

        public static async Task<List<LessonsByCourseViewModel>> GetLessonsByCourse(int courseId)
        {
            List<LessonsByCourseViewModel> lessons = new List<LessonsByCourseViewModel>();

            string query = @"SELECT * FROM vwLessonsWithCourses
                            WHERE CourseId = @CourseId 
	                        ORDER BY CreatedAt;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int titleIndex = reader.GetOrdinal("Title");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int thumbnailUrlIndex = reader.GetOrdinal("ThumbnailUrl");
                    int isPublishedIndex = reader.GetOrdinal("IsPublished");
                    int instructorIdIndex = reader.GetOrdinal("InstructorId");
                    int instructorNameIndex = reader.GetOrdinal("InstructorName");
                    int courseIdIndex = reader.GetOrdinal("CourseId");

                    while (await reader.ReadAsync())
                    {
                        lessons.Add(new LessonsByCourseViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            Title = reader.GetString(titleIndex),
                            Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                            BasePrice = reader.GetDecimal(basePriceIndex),
                            CreatedAt = reader.GetDateTime(createdAtIndex),
                            ThumbnailUrl = reader.IsDBNull(thumbnailUrlIndex) ? null : reader.GetString(thumbnailUrlIndex),
                            IsPublished = reader.GetBoolean(isPublishedIndex),
                            InstructorId = reader.GetInt32(instructorIdIndex),
                            InstructorName = reader.GetString(instructorNameIndex),
                            CourseId = reader.GetInt32(courseIdIndex),
                        });
                    }
                }
            }

            return lessons;
        }
    }
}
