using Common.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Common.Dtos;
namespace EduCore_DataAccess
{
    public class clsCoursesData
    {
        public static async Task<int> AddCourse(DtoCourse course, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"INSERT INTO Courses (ProductId, CoverImageUrl)
                            VALUES (@ProductId, @CoverImageUrl);
                            SELECT SCOPE_IDENTITY();";

            int courseId = -1;

            using (SqlCommand command = new SqlCommand(query, conn, tx))
            {
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = course.ProductId;
                command.Parameters.Add("@CoverImageUrl", SqlDbType.NVarChar, 500)
                    .Value = (object?)course.CoverImageUrl ?? DBNull.Value;

                var result = await command.ExecuteScalarAsync();

                courseId = result != null ? Convert.ToInt32(result) : -1;

            }

            return courseId;
        }

        public static async Task<int> AddCourse(DtoCourse course)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await conn.OpenAsync();
                return await AddCourse(course, conn, null);
            }
        }
        private static async Task<DtoCourse?> GetCourseInternal(int CourseId, bool IncludeDeleted = false)
        {
            if (CourseId <= 0) return null;
            DtoCourse? course = null;
            string query = @"SELECT Id, ProductId, CoverImageUrl, IsDeleted, DeletedAt, DeletedById
                            FROM Courses 
                            WHERE Id = @Id AND (@IncludeDeleted = 1 OR IsDeleted = 0)";
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = CourseId;
                command.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int productIndex = reader.GetOrdinal("ProductId");
                        int coverIndex = reader.GetOrdinal("CoverImageUrl");
                        int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                        int DeletedAtIndex = reader.GetOrdinal("DeletedAt");
                        int DeletedByIdIndex = reader.GetOrdinal("DeletedById");
                        course = new DtoCourse
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            CoverImageUrl = reader.IsDBNull(coverIndex) ? null : reader.GetString(coverIndex),
                            IsDeleted = reader.GetBoolean(isDeletedIndex),
                            DeletedAt = reader.IsDBNull(DeletedAtIndex) ? null : reader.GetDateTime(DeletedAtIndex),
                            DeletedById = reader.IsDBNull(DeletedByIdIndex) ? null : reader.GetInt32(DeletedByIdIndex),
                        };
                    }
                }
            }

            return course;
        }
        public static async Task<DtoCourse> GetCourseById(int CourseId)
            => await GetCourseInternal(CourseId);
        public static async Task<DtoCourse> GetCourseByIdIncludeDeleted(int CourseId)
            => await GetCourseInternal(CourseId, true);

        public static async Task<DtoCourse> GetCoursebyProductId(int productId)
        {
            if (productId <= 0) return null;
            DtoCourse course = null;
            string query = @"SELECT Id, ProductId, Summary, CoverImageUrl, IsDeleted, DeletedAt, DeletedById
                            FROM Courses
                            WHERE ProductId = @ProductId AND IsDeleted = 0";
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
                        int coverIndex = reader.GetOrdinal("CoverImageUrl");
                        int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                        int DeletedAtIndex = reader.GetOrdinal("DeletedAt");
                        int DeletedByIdIndex = reader.GetOrdinal("DeletedById");

                        course = new DtoCourse
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            CoverImageUrl = reader.IsDBNull(coverIndex) ? null : reader.GetString(coverIndex),
                            IsDeleted = reader.GetBoolean(isDeletedIndex),
                            DeletedAt = reader.IsDBNull(DeletedAtIndex) ? null : reader.GetDateTime(DeletedAtIndex),
                            DeletedById = reader.IsDBNull(DeletedByIdIndex) ? null : reader.GetInt32(DeletedByIdIndex)
                        };
                    }
                }
            }

            return course;
        }
        public static async Task<bool> UpdateCourse(DtoCourse course, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Courses
                             SET CoverImageUrl = @CoverImageUrl
                             WHERE Id = @Id;";

            int rows = 0;

            using (SqlCommand command = new SqlCommand(query, conn, tx))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = course.Id;
                command.Parameters.Add("@CoverImageUrl", SqlDbType.NVarChar, 500)
                    .Value = (object?)course.CoverImageUrl ?? DBNull.Value;

                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        public static async Task<bool> UpdateCourse(DtoCourse course)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                await conn.OpenAsync();
                return await UpdateCourse(course, conn, null);
            }
        }
        private static async Task<bool> InternalDeleteCourse(int courseId, int AdminId, SqlConnection conn, SqlTransaction tx, bool UnDelete = false)
        {
            string query = @"UPDATE Courses
                            SET IsDeleted = 1,
                            DeletedAt = GetDATE(),
                            DeletedById = @AdminId
                            WHERE Id = @Id;";
            if (UnDelete)
            {
                query = @"UPDATE Courses
                            SET IsDeleted = 0,
                            DeletedAt = NULL,
                            DeletedById = NULL
                            WHERE Id = @Id;";
            }


            int rows = 0;

            using (SqlCommand command = new SqlCommand(query, conn, tx))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = courseId;
                command.Parameters.Add("@AdminId", SqlDbType.Int).Value = AdminId;

                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        public static async Task<bool> DeleteCourse(int courseId, int adminId, SqlConnection conn, SqlTransaction tx)
        {
            return await InternalDeleteCourse(courseId, adminId, conn, tx);
        }

        public static async Task<bool> UnDelete(int courseId, int adminId, SqlConnection conn, SqlTransaction tx)
        {
            return await InternalDeleteCourse(courseId, adminId, conn, tx, true);
        }

        private static async Task<List<DtoCourse>> GetAllCoursesInternal(int pageNumber, int pageSize, bool IncludeDeleted = false)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoCourse> courses = new List<DtoCourse>();
            string query = @"SELECT Id, ProductId, CoverImageUrl, IsDeleted, DeletedAt, DeletedById
                            FROM Courses
                            WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
                            ORDER BY Id
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";


            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                sqlCommand.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;


                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIndex = reader.GetOrdinal("ProductId");
                    int coverIndex = reader.GetOrdinal("CoverImageUrl");
                    int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                    int DeletedAtIndex = reader.GetOrdinal("DeletedAt");
                    int DeletedByIdIndex = reader.GetOrdinal("DeletedById");

                    while (await reader.ReadAsync())
                    {

                        courses.Add(new DtoCourse
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            CoverImageUrl = reader.IsDBNull(coverIndex) ? null : reader.GetString(coverIndex),
                            IsDeleted = reader.GetBoolean(isDeletedIndex),
                            DeletedAt = reader.IsDBNull(DeletedAtIndex) ? null : reader.GetDateTime(DeletedAtIndex),
                            DeletedById = reader.IsDBNull(DeletedByIdIndex) ? null : reader.GetInt32(DeletedByIdIndex)

                        });
                    }
                }
            }

            return courses;
        }

        public static async Task<List<DtoCourse>> GetAllCourses(int pageNumber, int pageSize)
            => await GetAllCoursesInternal(pageNumber, pageSize);
        public static async Task<List<DtoCourse>> GetAllCoursesIncludeDeleted(int pageNumber, int pageSize)
            => await GetAllCoursesInternal(pageNumber, pageSize, true);

        private static async Task<List<CourseWithInstructorViewModel>> GetAllCoursesWithInstructorViewModelInternal(int pageNumber, int pageSize,bool IncludeDeleted = false, string SearchText = "")
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            string query = @"WITH PagedCourses AS (
                                SELECT C.Id
                                FROM Courses C
                                JOIN Products P ON C.ProductId = P.Id
                                ORDER BY P.CreatedAt DESC
                                OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                                FETCH NEXT @RowsPerPage ROWS ONLY
                            )
                            SELECT 
                                C.Id        AS Id,
                                P.Name      AS Title,
                                P.Summary,
                                P.BasePrice,
                                P.CreatedAt,
                                P.ThumbnailUrl,
                                C.CoverImageUrl,
                                C.IsDeleted,
                                U.Id        AS InstructorId,
                                U.Name      AS InstructorName
                            FROM PagedCourses PC
                            INNER JOIN Courses C              ON PC.Id = C.Id
                            INNER JOIN Products P             ON C.ProductId = P.Id
                            LEFT JOIN CoursesInstructors CI  ON C.Id = CI.CourseId
                            LEFT JOIN Users U                ON CI.InstructorId = U.Id
                            WHERE (@IncludeDeleted = 1 OR C.IsDeleted = 0) AND 
                            (@SearchText IS NULL OR P.Name LIKE @SearchText OR U.Name LIKE @SearchText)

                            ORDER BY P.CreatedAt DESC;";

            var coursesDict = new Dictionary<int, CourseWithInstructorViewModel>();

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                sqlCommand.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;
                sqlCommand.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = String.IsNullOrWhiteSpace(SearchText) ? DBNull.Value : $"%{SearchText}%";
                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int titleIndex = reader.GetOrdinal("Title");
                    int summaryIndex = reader.GetOrdinal("Summary");
                    int basePriceIndex = reader.GetOrdinal("BasePrice");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");
                    int thumbnailIndex = reader.GetOrdinal("ThumbnailUrl");
                    int coverIndex = reader.GetOrdinal("CoverImageUrl");
                    int instrIdIndex = reader.GetOrdinal("InstructorId");
                    int instrNameIndex = reader.GetOrdinal("InstructorName");
                    int isDeletedIndex = reader.GetOrdinal("IsDeleted");

                    while (await reader.ReadAsync())
                    {
                        int courseId = reader.GetInt32(idIndex);

                        if (!coursesDict.TryGetValue(courseId, out var course))
                        {
                            course = new CourseWithInstructorViewModel
                            {
                                Id = courseId,
                                Title = reader.GetString(titleIndex),
                                Summary = reader.IsDBNull(summaryIndex) ? null : reader.GetString(summaryIndex),
                                BasePrice = reader.GetDecimal(basePriceIndex),
                                CreatedAt = reader.GetDateTime(createdAtIndex),
                                ThumbnailUrl = reader.IsDBNull(thumbnailIndex) ? null : reader.GetString(thumbnailIndex),
                                CoverImageUrl = reader.IsDBNull(coverIndex) ? null : reader.GetString(coverIndex),
                                IsDeleted = reader.GetBoolean(isDeletedIndex),
                                CourseInstructors = new List<InstructorsViewModel>()
                            };
                            coursesDict.Add(courseId, course);
                        }
                        if (!reader.IsDBNull(instrIdIndex))
                        {
                            course.CourseInstructors.Add(new InstructorsViewModel
                            {
                                InstructorId = reader.GetInt32(instrIdIndex),
                                InstructorName = reader.GetString(instrNameIndex)
                            });
                        }
                    }
                }
            }

            return coursesDict.Values.ToList();
        }


        public static async Task<List<CourseWithInstructorViewModel>> GetAllCoursesWithInstructorViewModel(int pageNumber, int pageSize, string SearchText = "")
            => await GetAllCoursesWithInstructorViewModelInternal(pageNumber,pageSize,false ,SearchText);

        public static async Task<List<CourseWithInstructorViewModel>> GetAllCoursesWithInstructorViewModelWithDeleted(int pageNumber, int pageSize, string SearchText = "")
            => await GetAllCoursesWithInstructorViewModelInternal(pageNumber, pageSize, true, SearchText);
        public static async Task<bool> CourseExists(int courseId)
        {
            const string query = @"SELECT CAST(
                                CASE WHEN EXISTS (
                                    SELECT 1 FROM Courses
                                    WHERE Id = @Id AND IsDeleted = 0
                                )
                                THEN 1 ELSE 0 END
                            AS BIT)";

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = courseId;

                await connection.OpenAsync();

                object? result = await command.ExecuteScalarAsync();
                return result != null && (bool)result;
            }
        }


        public static async Task<bool> AssignLessonToCourse(int courseId, int lessonId)
        {
            string query = @"INSERT INTO CoursesLessons(CourseId,LessonId)
                            VALUES(@courseId,@lessonId)";
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, conn))
            {
                command.Parameters.Add("@courseId", SqlDbType.Int).Value = courseId;
                command.Parameters.Add("@lessonId", SqlDbType.Int).Value = lessonId;
                await conn.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();

                return rowsAffected > 0;

            }
        }
    }
}
