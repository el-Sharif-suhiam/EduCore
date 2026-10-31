using Common.ViewModels;
using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsProgressData
    {
        public static async Task<bool> UpsertProgress(DtoProgress progress)
        {
            // Atomic upsert: update first, insert only when the update
            // matched nothing. The PK (UserId, LessonId) is the arbiter —
            // two concurrent calls can never duplicate or dead-lock on
            // the old IF EXISTS / INSERT pattern.
            string query = @"SET XACT_ABORT ON;
                            BEGIN TRY
                                BEGIN TRANSACTION;

                                -- Acquire a key-range lock on the (UserId, LessonId)
                                -- PK so concurrent upserts serialize per key.
                                SELECT 1 FROM Progress WITH (UPDLOCK, HOLDLOCK)
                                WHERE UserId = @UserId AND LessonId = @LessonId;

                                UPDATE Progress
                                SET IsComplete = @IsComplete,
                                    CompletedDate = CASE
                                                        WHEN @IsComplete = 1 THEN SYSUTCDATETIME()
                                                        ELSE CompletedDate
                                                    END
                                WHERE UserId = @UserId AND LessonId = @LessonId;

                                IF @@ROWCOUNT = 0
                                    INSERT INTO Progress (UserId, LessonId, IsComplete)
                                    VALUES (@UserId, @LessonId, @IsComplete);

                                COMMIT;
                            END TRY
                            BEGIN CATCH
                                IF @@TRANCOUNT > 0
                                    ROLLBACK;
                                THROW;
                            END CATCH";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = progress.UserId;
                cmd.Parameters.Add("@LessonId", SqlDbType.Int).Value = progress.LessonId;
                cmd.Parameters.Add("@IsComplete", SqlDbType.Bit)
                    .Value = (object?)progress.IsComplete ?? DBNull.Value;

                await con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
        }

        public static async Task<DtoProgress> GetProgress(int userId, int lessonId)
        {
            string query = @"SELECT UserId, LessonId, CompletedDate, IsComplete
                         FROM Progress
                         WHERE UserId = @UserId AND LessonId = @LessonId;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@LessonId", SqlDbType.Int).Value = lessonId;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int userIdIndex = reader.GetOrdinal("UserId");
                        int lessonIdIndex = reader.GetOrdinal("LessonId");
                        int completedDateIndex = reader.GetOrdinal("CompletedDate");
                        int isCompleteIndex = reader.GetOrdinal("IsComplete");

                        return new DtoProgress
                        {
                            UserId = reader.GetInt32(userIdIndex),
                            LessonId = reader.GetInt32(lessonIdIndex),
                            CompletedDate = reader.IsDBNull(completedDateIndex) ? null : reader.GetDateTime(completedDateIndex),
                            IsComplete = reader.IsDBNull(isCompleteIndex) ? null : reader.GetBoolean(isCompleteIndex)
                        };
                    }
                }
            }

            return null;
        }


        public static async Task<CourseProgressViewModel> GetCourseProgress(int userId, int courseId)
        {
            string query = @"SELECT 
                                 l.Id AS LessonId,
                                 l.Title,
                                 p.IsComplete,
                                 p.CompletedDate
                             FROM CoursesLessons cl
                             JOIN Lessons l ON l.Id = cl.LessonId AND l.IsDeleted = 0
                             LEFT JOIN Progress p
                                 ON p.LessonId = l.Id AND p.UserId = @UserId
                             WHERE cl.CourseId = @CourseId;";

            CourseProgressViewModel result = new CourseProgressViewModel
            {
                CourseId = courseId,
                UserId = userId,
                Lessons = new List<LessonProgressVM>()
            };

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@CourseId", SqlDbType.Int).Value = courseId;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int titleIndex = reader.GetOrdinal("Title");
                    int lessonIdIndex = reader.GetOrdinal("LessonId");
                    int completedDateIndex = reader.GetOrdinal("CompletedDate");
                    int isCompleteIndex = reader.GetOrdinal("IsComplete");

                    while (await reader.ReadAsync())
                    {
                        result.Lessons.Add(new LessonProgressVM
                        {
                            LessonId = reader.GetInt32(lessonIdIndex),
                            Title = reader.GetString(titleIndex),
                            IsComplete = !reader.IsDBNull(isCompleteIndex) && reader.GetBoolean(isCompleteIndex),
                            CompletedDate = reader.IsDBNull(completedDateIndex) ? null : reader.GetDateTime(completedDateIndex)
                        });
                    }
                }
            }

            // count the stats
            result.TotalLessons = result.Lessons.Count;
            result.CompletedLessons = result.Lessons.Count(l => l.IsComplete);

            result.ProgressPercentage =
                result.TotalLessons == 0 ? 0 :
                (double)result.CompletedLessons / result.TotalLessons * 100;

            return result;
        }
    }
}
