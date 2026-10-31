using Common.ViewModels;
using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsCourseLessonData
    {
        public static async Task<bool> AddLessonToCourse(DtoCourseLesson courseLesson)
        {
            string query = @"INSERT INTO CoursesLessons 
                        (CourseId,LessonId)
                        VALUES 
                        (@CourseId,@LessonId);";
            int rows = 0;
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseLesson.CourseId;
                command.Parameters.Add("@LessonId", SqlDbType.Int).Value = courseLesson.LessonId;
                await connection.OpenAsync();

                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        // ============================================================
        // Transaction-aware variants used by clsLesson.Save so the two
        // representations of the course-lesson relationship
        // (Lessons.CourseId + CoursesLessons) are always written inside
        // ONE transaction and can never drift apart.
        // ============================================================
        public static async Task AddLessonToCourse(int courseId, int lessonId, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"INSERT INTO CoursesLessons (CourseId, LessonId)
                             VALUES (@CourseId, @LessonId);";

            using (SqlCommand command = new SqlCommand(query, conn, tx))
            {
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;
                command.Parameters.Add("@LessonId", SqlDbType.Int).Value = lessonId;
                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Removes every junction row for the lesson, then re-inserts the
        /// current association when <paramref name="courseId"/> is present.
        /// Keeps CoursesLessons in exact sync with Lessons.CourseId.
        /// </summary>
        public static async Task SyncLessonCourse(int lessonId, int? courseId, SqlConnection conn, SqlTransaction tx)
        {
            string query = @"DELETE FROM CoursesLessons WHERE LessonId = @LessonId;";

            if (courseId.HasValue)
            {
                query += @"INSERT INTO CoursesLessons (CourseId, LessonId)
                           VALUES (@CourseId, @LessonId);";
            }

            using (SqlCommand command = new SqlCommand(query, conn, tx))
            {
                command.Parameters.Add("@LessonId", SqlDbType.Int).Value = lessonId;
                command.Parameters.Add("@CourseId", SqlDbType.Int)
                    .Value = (object?)courseId ?? DBNull.Value;
                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task<bool> DeleteLessonFromCourse(DtoCourseLesson courseLesson)
        {
            string query = @"DELETE FROM CoursesLessons 
                         WHERE CourseId = @CourseId AND LessonId = @LessonId;";
            int rows = 0;
            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseLesson.CourseId;
                command.Parameters.Add("@LessonId", SqlDbType.Int).Value = courseLesson.LessonId;
                await connection.OpenAsync();

                rows = await command.ExecuteNonQueryAsync();
            }

            return rows > 0;
        }

        public static async Task<List<CourseLessonViewModel>> GetAllCourseLessonsViewModel(int courseId,int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<CourseLessonViewModel> courseLessons = new List<CourseLessonViewModel>();
            string query = @"SELECT  L.Id, L.ProductId, L.Title , L.VideoUrl, L.BodyText, L.InstructorId, CL.CourseId, P.CreatedAt FROM Lessons L
                            JOIN CoursesLessons CL ON L.Id = CL.LessonId
                            JOIN Products P ON L.ProductId = P.Id
                            WHERE CL.CourseId = @CourseId
                            ORDER BY CreatedAt
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";


            using (SqlConnection sqlConnection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
            {
                sqlCommand.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                sqlCommand.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;
                sqlCommand.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;


                await sqlConnection.OpenAsync();

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int productIdIndex = reader.GetOrdinal("ProductId");
                    int titleIndex = reader.GetOrdinal("Title");
                    int videoUrlIndex = reader.GetOrdinal("VideoUrl");
                    int bodyTextIndex = reader.GetOrdinal("BodyText");

                    int instructorIdIndex = reader.GetOrdinal("InstructorId");
                    int courseIdIndex = reader.GetOrdinal("CourseId");
                    int createdAtIndex = reader.GetOrdinal("CreatedAt");

                    while (await reader.ReadAsync())
                    {

                        courseLessons.Add(new CourseLessonViewModel
                        {
                            Id = reader.GetInt32(idIndex),
                            ProductId = reader.GetInt32(productIdIndex),
                            Title = reader.GetString(titleIndex),
                            VideoUrl = reader.IsDBNull(videoUrlIndex) ? null : reader.GetString(videoUrlIndex),
                            BodyText = reader.IsDBNull(bodyTextIndex) ? null : reader.GetString(bodyTextIndex),
                            CourseId = reader.GetInt32(courseIdIndex),
                            InstructorId = reader.GetInt32(instructorIdIndex)
                        });
                    }
                }
            }

            return courseLessons;
        }
    }
}
