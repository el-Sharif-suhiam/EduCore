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
        public static int AddCourse(DtoCourse course, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"INSERT INTO Courses (ProductId, CoverImageUrl)
                            VALUES (@ProductId, @CoverImageUrl);
                            SELECT SCOPE_IDENTITY();";

            int courseId = -1;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@ProductId", SqlDbType.Int).Value = course.ProductId;
                command.Parameters.Add("@CoverImageUrl", SqlDbType.NVarChar, 500)
                    .Value = (object?)course.CoverImageUrl ?? DBNull.Value;

                var result = command.ExecuteScalar();

                courseId = result != null ? Convert.ToInt32(result) : -1;

            }

            return courseId;
        }

        public static int AddCourse(DtoCourse course)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                conn.Open();
                return AddCourse(course, conn, null);
            }
        }
        private static DtoCourse GetCourseInternal(int CourseId, bool IncludeDeleted = false)
        {
            if (CourseId <= 0) return null;
            DtoCourse course = null;
            string query = @"SELECT Id, ProductId, Summary, CoverImageUrl, IsDeleted, DeletedAt, DeletedById
                            FROM Courses
                            WHERE Id = @Id AND (@IncludeDeleted = 1 OR IsDeleted = 0)";
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = CourseId;
                command.Parameters.Add("@IncludeDeleted", SqlDbType.Bit).Value = IncludeDeleted;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int productIndex = reader.GetOrdinal("ProductId");
                        int summaryIndex = reader.GetOrdinal("Summary");
                        int coverIndex = reader.GetOrdinal("CoverImageUrl");
                        int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                        int DeletedAtIndex = reader.GetOrdinal("DeletedAt");
                        int DeletedByIdIndex = reader.GetOrdinal("DeletedById");

                        course = new DtoCourse
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIndex),
                            Summary = reader.GetString(summaryIndex),
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
        public static DtoCourse GetCourseById(int CourseId)
            => GetCourseInternal(CourseId);
        public static DtoCourse GetCourseByIdIncludeDeleted(int CourseId)
            => GetCourseInternal(CourseId, true);
        public static bool UpdateCourse(DtoCourse course, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"UPDATE Courses
                             SET CoverImageUrl = @CoverImageUrl
                             WHERE Id = @Id;";

            int rows = 0;

            using (SqlCommand command = new SqlCommand(query, conn,tx))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = course.Id;
                command.Parameters.Add("@CoverImageUrl", SqlDbType.NVarChar, 500)
                    .Value = (object?)course.CoverImageUrl ?? DBNull.Value;

                rows = command.ExecuteNonQuery();
            }

            return rows > 0;
        }

        public static bool UpdateCourse(DtoCourse course)
        {
            using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                conn.Open();
                return UpdateCourse(course, conn, null);
            }
        }
        private static bool InternalDeleteCourse(int courseId, int AdminId,bool UnDelete = false)
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

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = courseId;
                command.Parameters.Add("@AdminId", SqlDbType.Int).Value = AdminId;

                connection.Open();
                rows = command.ExecuteNonQuery();
            }

            return rows > 0;
        }

        public static bool DeleteCourse(int courseId, int adminId)
        {
            return InternalDeleteCourse(courseId, adminId);
        }

        public static bool UnDelete(int courseId, int adminId)
        {
            return InternalDeleteCourse(courseId, adminId, true);
        }

        private static List<DtoCourse> GetAllCoursesInternal(int pageNumber, int pageSize, bool IncludeDeleted = false)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoCourse> courses = new List<DtoCourse>();
            string query = @"Id, ProductId, CoverImageUrl, IsDeleted, DeletedAt, DeletedById
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


                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIndex = reader.GetOrdinal("ProductId");
                    int coverIndex = reader.GetOrdinal("CoverImageUrl");
                    int isDeletedIndex = reader.GetOrdinal("IsDeleted");
                    int DeletedAtIndex = reader.GetOrdinal("DeletedAt");
                    int DeletedByIdIndex = reader.GetOrdinal("DeletedById");

                    while (reader.Read())
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

        public static List<DtoCourse> GetAllCourses(int pageNumber, int pageSize)
            => GetAllCoursesInternal(pageNumber, pageSize);
        public static List<DtoCourse> GetAllCoursesIncludeDeleted(int pageNumber, int pageSize) 
            => GetAllCoursesInternal(pageNumber, pageSize, true);

        public static List<CourseWithInstructorViewModel> GetAllCoursesWithInstructorViewModel(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            string query = @"WITH PagedCourses AS (
                                SELECT C.Id
                                FROM Courses C
                                JOIN Products P ON C.ProductId = P.Id
                                WHERE C.IsDeleted = 0
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
                                U.Id        AS InstructorId,
                                U.Name      AS InstructorName
                            FROM PagedCourses PC
                            JOIN Courses C              ON PC.Id = C.Id
                            JOIN Products P             ON C.ProductId = P.Id
                            JOIN CoursesInstructors CI  ON C.Id = CI.CourseId
                            JOIN Users U                ON CI.InstructorId = U.Id
                            ORDER BY P.CreatedAt DESC;";

            var coursesDict = new Dictionary<int, CourseWithInstructorViewModel>();

            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;

                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
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

                    while (reader.Read())
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
                                CourseInstructors = new List<InstructorsViewModel>()
                            };
                            coursesDict.Add(courseId, course);
                        }

                        course.CourseInstructors.Add(new InstructorsViewModel
                        {
                            InstructorId = reader.GetInt32(instrIdIndex),
                            InstructorName = reader.GetString(instrNameIndex)
                        });
                    }
                }
            }

            return coursesDict.Values.ToList();
        }

        public static bool CourseExists(int courseId)
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

                connection.Open();
                return (bool)command.ExecuteScalar();
            }
        }
    }
}
