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
        public static int AddLesson(DtoLessons lesson, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"INSERT INTO Lessons 
                        (ProductId, Title, VideoUrl, BodyText, InstructorId)
                        VALUES 
                        (@ProductId, @Title, @VideoUrl, @BodyText, @InstructorId);
                        SELECT SCOPE_IDENTITY();";

            int lessonId = -1;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = lesson.ProductId;
                command.Parameters.Add("@Title", SqlDbType.NVarChar, 150).Value = lesson.Title;

                command.Parameters.Add("@VideoUrl", SqlDbType.VarChar, 500).Value = (object?)lesson.VideoUrl ?? DBNull.Value;
                command.Parameters.Add("@BodyText", SqlDbType.NVarChar, -1).Value = (object?)lesson.BodyText ?? DBNull.Value;

                command.Parameters.Add("@InstructorId", SqlDbType.Int).Value = lesson.InstructorId;

                var result = command.ExecuteScalar();

                lessonId = result != null ? Convert.ToInt32(result) : -1;

            }

            return lessonId;
        }


        private static DtoLessons GetLessonInternal(int lessonId, bool IncludeDeleted = false)
        {
            if (lessonId <= 0) return null;

            DtoLessons lesson = null;

            string query = @"SELECT Id, ProductId, Title, VideoUrl, BodyText, 
                                IsDeleted, DeletedAt, DeletedById, InstructorId
                         FROM Lessons
                         WHERE Id = @Id 
                         AND (@IncludeDeleted = 1 OR IsDeleted = 0);";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = lessonId;
                command.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
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

        public static DtoLessons GetLessonById(int lessonId)
            => GetLessonInternal(lessonId);

        public static DtoLessons GetLessonByIdIncludeDeleted(int lessonId)
            => GetLessonInternal(lessonId, true);

        public static bool UpdateLesson(DtoLessons lesson, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Lessons
                         SET Title = @Title,
                             VideoUrl = @VideoUrl,
                             BodyText = @BodyText,
                             InstructorId = @InstructorId
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

                rows = command.ExecuteNonQuery();
            }

            return rows > 0;
        }

        private static bool ControlDeleteLesson(int lessonId, int AdminId, bool UnDelete = false)
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

                connection.Open();
                rows = command.ExecuteNonQuery();
            }

            return rows > 0;
        }

        public static bool DeleteLesson(int lessonId,int adminId)
        {
            return ControlDeleteLesson(lessonId, adminId);
        }

        public static bool UnDeleteLesson(int lessonId, int adminId)
        {
            return ControlDeleteLesson(lessonId,adminId,true);
        }

        private static List<DtoLessons> GetAllLessonsInternal(int pageNumber, int pageSize, bool IncludeDeleted = false)
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

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
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

                    while (reader.Read())
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

        public static List<DtoLessons> GetAllLessons(int pageNumber, int pageSize)
            => GetAllLessonsInternal(pageNumber, pageSize);

        public static List<DtoLessons> GetAllLessonsIncludeDeleted(int pageNumber, int pageSize)
            => GetAllLessonsInternal(pageNumber, pageSize, true);

        public static List<LessonsWithOutCoursesViewModel> GetAllLessonsWithOutCourses(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<LessonsWithOutCoursesViewModel> lessons = new List<LessonsWithOutCoursesViewModel>();

            string query = @"SELECT * FROM vwLessonsWithOutCourses 
	                      ORDER BY CreatedAt
                         OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                         FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                command.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int nameIndex = reader.GetOrdinal("Name");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int createAtIndex = reader.GetOrdinal("CreateAt");
                    int thumbnailUrlIndex = reader.GetOrdinal("ThumbnailUrl");
                    int isPublishedIndex = reader.GetOrdinal("IsPublished");
                    int instructorIdIndex = reader.GetOrdinal("InstructorId");
                    int instructorNameIndex = reader.GetOrdinal("InstructorId");

                    while (reader.Read())
                    {
                        lessons.Add(new LessonsWithOutCoursesViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            Name = reader.GetString(nameIndex),
                            Summary = reader.GetString(summaryIndex),
                            BasePrice = reader.GetDecimal(basePriceIndex),
                            CreateAt = reader.GetDateTime(createAtIndex),
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

        public static List<LessonsByCourseViewModel> GetLessonsByCourse(int courseId)
        {
            List<LessonsByCourseViewModel> lessons = new List<LessonsByCourseViewModel>();

            string query = @"SELECT * FROM vwLessonsWithCourses
                            WHERE CourseId = @CourseId 
	                        ORDER BY CreatedAt;";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int nameIndex = reader.GetOrdinal("Name");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int createAtIndex = reader.GetOrdinal("CreateAt");
                    int thumbnailUrlIndex = reader.GetOrdinal("ThumbnailUrl");
                    int isPublishedIndex = reader.GetOrdinal("IsPublished");
                    int instructorIdIndex = reader.GetOrdinal("InstructorId");
                    int instructorNameIndex = reader.GetOrdinal("InstructorId");
                    int courseIdIndex = reader.GetOrdinal("CourseId");

                    while (reader.Read())
                    {
                        lessons.Add(new LessonsByCourseViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            Name = reader.GetString(nameIndex),
                            Summary = reader.GetString(summaryIndex),
                            BasePrice = reader.GetDecimal(basePriceIndex),
                            CreateAt = reader.GetDateTime(createAtIndex),
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
