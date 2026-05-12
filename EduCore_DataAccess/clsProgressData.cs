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
        public static bool UpsertProgress(DtoProgress progress)
        {
            string query = @"IF EXISTS (SELECT 1 FROM Progress WHERE UserId = @UserId AND LessonId = @LessonId)
                            BEGIN
                                UPDATE Progress
                                SET IsComplete = @IsComplete,
                                    CompletedDate = CASE 
                                                        WHEN @IsComplete = 1 THEN GETDATE()
                                                        ELSE CompletedDate
                                                    END
                                WHERE UserId = @UserId AND LessonId = @LessonId;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO Progress (UserId, LessonId, IsComplete)
                                VALUES (@UserId, @LessonId, @IsComplete);
                            END;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = progress.UserId;
                cmd.Parameters.Add("@LessonId", SqlDbType.Int).Value = progress.LessonId;
                cmd.Parameters.Add("@IsComplete", SqlDbType.Bit)
                    .Value = (object?)progress.IsComplete ?? DBNull.Value;

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static DtoProgress GetProgress(int userId, int lessonId)
        {
            string query = @"SELECT UserId, LessonId, CompletedDate, IsComplete
                         FROM Progress
                         WHERE UserId = @UserId AND LessonId = @LessonId;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@LessonId", SqlDbType.Int).Value = lessonId;

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new DtoProgress
                        {
                            UserId = reader.GetInt32(0),
                            LessonId = reader.GetInt32(1),
                            CompletedDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                            IsComplete = reader.IsDBNull(3) ? null : reader.GetBoolean(3)
                        };
                    }
                }
            }

            return null;
        }


        public static CourseProgressViewModel GetCourseProgress(int userId, int courseId)
        {
            string query = @"SELECT 
                                 l.Id,
                                 l.Title,
                                 p.IsComplete,
                                 p.CompletedDate
                             FROM CourseLessons cl
                             JOIN Lessons l ON l.Id = cl.LessonId
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

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Lessons.Add(new LessonProgressVM
                        {
                            LessonId = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            IsComplete = !reader.IsDBNull(2) && reader.GetBoolean(2),
                            CompletedDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
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
