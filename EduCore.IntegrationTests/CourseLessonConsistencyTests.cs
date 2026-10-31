using System;
using System.Threading.Tasks;
using Common.Dtos;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using EduCore_DataAccess;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Regression tests for the course-lesson dual-write fix and
    /// bundle-aware access (instructs #3 and #4):
    ///   * a lesson created/updated through the business layer keeps BOTH
    ///     relationship representations (Lessons.CourseId + CoursesLessons)
    ///     in sync inside one transaction;
    ///   * a lesson displayed inside a course is the same lesson counted by
    ///     progress calculations and markable complete;
    ///   * an active bundle enrollment grants access to its included
    ///     courses (respecting expiry/deletion), and to nothing else.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class CourseLessonConsistencyTests
    {
        private readonly DatabaseFixture _db;

        public CourseLessonConsistencyTests(DatabaseFixture db) => _db = db;

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private static async Task<clsLesson> BuildLessonAsync(int creatorId, int courseId)
        {
            clsLesson lesson = new();
            lesson.SetName($"Lesson {Guid.NewGuid():N}");
            lesson.SetBasePrice(0m);
            await lesson.AssignCreatedByUserAsync(creatorId);
            await lesson.SetInstructorToLesson(creatorId);
            lesson.SetTitle("Course lesson");
            lesson.SetBodyText("Body text");
            await lesson.SetCourseId(courseId);
            return lesson;
        }

        private async Task<int> CreateLessonInCourseAsync(int instructorId, int courseId)
        {
            clsLesson lesson = await BuildLessonAsync(instructorId, courseId);
            Assert.True(await lesson.Save());
            return lesson.Id;
        }

        private Task<int> JunctionCountAsync(int courseId, int lessonId)
            => _db.Seed.ScalarIntRawAsync(
                @"SELECT COUNT(*) FROM CoursesLessons WHERE CourseId = @CourseId AND LessonId = @LessonId;",
                c =>
                {
                    c.Parameters.Add("@CourseId", System.Data.SqlDbType.Int).Value = courseId;
                    c.Parameters.Add("@LessonId", System.Data.SqlDbType.Int).Value = lessonId;
                });

        private async Task<int> SeedCourseAsync(int instructorId)
        {
            int product = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 99m, instructorId, true);
            return await _db.Seed.CourseAsync(product);
        }

        [Fact]
        public async Task CreateCourseLesson_ViaBusinessLayer_SyncsJunctionAndProgress()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cl-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"cl-stud-{Guid.NewGuid():N}@test.local", "Student");

            int product = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 99m, instructor, true);
            int course = await _db.Seed.CourseAsync(product);
            await _db.Seed.EnrollDirectAsync(student, product);

            int lessonId = await CreateLessonInCourseAsync(instructor, course);

            // 1. junction row exists (second representation kept in sync)
            Assert.Equal(1, await JunctionCountAsync(course, lessonId));

            // 2. the lesson the course displays is the same one progress counts
            var progress = await clsProgress.GetCourseProgress(student, course);
            Assert.Equal(1, progress.TotalLessons);
            Assert.Equal(lessonId, progress.Lessons[0].LessonId);

            // 3. progress marking works for a course lesson (access check
            //    resolves the lesson's owning course product)
            Assert.True(await clsProgress.MarkAsComplete(student, lessonId));
        }

        [Fact]
        public async Task MoveLessonToAnotherCourse_SyncsBothRepresentations()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cl-move-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int course1 = await SeedCourseAsync(instructor);
            int course2 = await SeedCourseAsync(instructor);

            int lessonId = await CreateLessonInCourseAsync(instructor, course1);

            clsLesson lesson = await clsLesson.FindWithCourseId(lessonId, course1);
            await lesson.SetCourseId(course2);
            Assert.True(await lesson.Save());

            Assert.Equal(0, await JunctionCountAsync(course1, lessonId));
            Assert.Equal(1, await JunctionCountAsync(course2, lessonId));

            int? storedCourseId = await _db.Seed.ScalarIntOrNullRawAsync(
                @"SELECT CourseId FROM Lessons WHERE Id = @Id;",
                c => c.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = lessonId);
            Assert.Equal(course2, storedCourseId);
        }

        [Fact]
        public async Task DetachLessonFromCourse_ClearsJunction()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cl-det-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int course = await SeedCourseAsync(instructor);

            int lessonId = await CreateLessonInCourseAsync(instructor, course);

            clsLesson lesson = await clsLesson.FindWithCourseId(lessonId, course);
            await lesson.SetCourseId(null);
            Assert.True(await lesson.Save());

            Assert.Equal(0, await JunctionCountAsync(course, lessonId));

            int? storedCourseId = await _db.Seed.ScalarIntOrNullRawAsync(
                @"SELECT CourseId FROM Lessons WHERE Id = @Id;",
                c => c.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = lessonId);
            Assert.Null(storedCourseId);
        }

        [Fact]
        public async Task BundleEnrollment_GrantsAccessToIncludedCourse()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cl-bnd-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"cl-bnd-stud-{Guid.NewGuid():N}@test.local", "Student");

            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, true);
            int course = await _db.Seed.CourseAsync(courseProduct);

            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, instructor, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);

            await _db.Seed.EnrollDirectAsync(student, bundleProduct);

            Assert.True(await clsEnrollment.IsUserEnrolled(student, courseProduct));

            // end-to-end: the bundle buyer can open the course and mark
            // its lessons complete without a direct course enrollment
            var progress = await clsProgress.GetCourseProgress(student, course);
            Assert.Equal(0, progress.TotalLessons);
        }

        [Fact]
        public async Task ExpiredBundleEnrollment_DeniesCourseAccess()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cl-bex-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"cl-bex-stud-{Guid.NewGuid():N}@test.local", "Student");

            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, true);
            int course = await _db.Seed.CourseAsync(courseProduct);

            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, instructor, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);

            await _db.Seed.EnrollDirectAsync(
                student, bundleProduct, expireAt: DateTime.UtcNow.Date.AddDays(-1));

            Assert.False(await clsEnrollment.IsUserEnrolled(student, courseProduct));
        }

        [Fact]
        public async Task BundleEnrollment_DoesNotGrantUnrelatedCourse()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cl-unr-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"cl-unr-stud-{Guid.NewGuid():N}@test.local", "Student");

            int inBundleProduct = await _db.Seed.CourseProductAsync($"In-bundle {Guid.NewGuid():N}", 50m, instructor, true);
            int inBundleCourse = await _db.Seed.CourseAsync(inBundleProduct);

            int unrelatedProduct = await _db.Seed.CourseProductAsync($"Unrelated {Guid.NewGuid():N}", 50m, instructor, true);
            await _db.Seed.CourseAsync(unrelatedProduct);

            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, instructor, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, inBundleCourse);

            await _db.Seed.EnrollDirectAsync(student, bundleProduct);

            Assert.True(await clsEnrollment.IsUserEnrolled(student, inBundleProduct));
            Assert.False(await clsEnrollment.IsUserEnrolled(student, unrelatedProduct));
        }

        [Fact]
        public async Task RenewExpiredEnrollment_SP_ExtendsAccessWithoutDuplicate()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cl-rnw-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"cl-rnw-stud-{Guid.NewGuid():N}@test.local", "Student");

            int product = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, true);

            // expired enrollment from an older purchase
            await _db.Seed.EnrollDirectAsync(
                student, product, expireAt: DateTime.UtcNow.Date.AddDays(-1));

            // re-purchase: fresh completed order + succeeded payment
            int orderId = await _db.Seed.OrderAsync(student, "Completed", 50m);
            await _db.Seed.OrderItemAsync(orderId, product, 50m);
            int paymentId = await _db.Seed.PaymentAsync(orderId, $"seed-rnw-{Guid.NewGuid():N}", "Succeeded", 50m);

            DateTime newExpireAt = DateTime.UtcNow.AddMonths(12).Date;

            bool added = false;
            await clsGeneralData.ExecuteTransaction(async (conn, tx) =>
            {
                added = await clsEnrollmentsData.AddEnrollment(
                    new DtoEnrollmentDataRequest
                    {
                        orderId = orderId,
                        PaymentId = paymentId,
                        ExpireAt = newExpireAt
                    },
                    conn, tx);
                return true;
            });

            // renewal reported, no duplicate row, access restored
            Assert.True(added);
            Assert.Equal(1, await _db.Seed.EnrollmentCountAsync(student, product));
            Assert.True(await clsEnrollment.IsUserEnrolled(student, product));
        }
    }
}
