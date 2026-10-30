using System;
using System.IO;
using System.Threading.Tasks;
using Common.Exceptions;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Regression tests for Issue A: the course-progress SQL selected l.Id
    /// but the reader called GetOrdinal("LessonId"), which threw on EVERY
    /// call. These tests exercise the real SQL + reader mapping on the
    /// disposable test database (not mocks), including the certificate
    /// eligibility path that depends on the calculation.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class ProgressTests
    {
        private readonly DatabaseFixture _db;

        public ProgressTests(DatabaseFixture db) => _db = db;

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private static string EnsureCertificateStorage()
        {
            string dir = Path.Combine(Path.GetTempPath(), "educore-test-certificates");
            Directory.CreateDirectory(dir);
            Environment.SetEnvironmentVariable("CERTIFICATES_STORAGE_PATH", dir);
            return dir;
        }

        /// <summary>Seeds instructor + student + course with N lessons; completes `completed` of them.</summary>
        private async Task<int> SeedCourse(int instructorId, int studentId, int lessonCount, int completed)
        {
            int product = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 99m, instructorId, true);
            int course = await _db.Seed.CourseAsync(product);

            for (int i = 0; i < lessonCount; i++)
            {
                int lesson = await _db.Seed.LessonAsync(
                    await _db.Seed.LessonProductAsync($"Lesson {i} {Guid.NewGuid():N}", 0m, instructorId, true),
                    $"Lesson {i}", instructorId, course);
                await _db.Seed.LinkCourseLesson(course, lesson);
            }

            // enrollment for the course product (required by the business layer)
            await _db.Seed.EnrollDirectAsync(studentId, product);

            // complete the first `completed` lessons
            if (completed > 0)
            {
                var lessons = await EduCore_DataAccess.clsLessonsData.GetLessonsByCourse(course);
                for (int i = 0; i < Math.Min(completed, lessonCount); i++)
                    await _db.Seed.ProgressAsync(studentId, lessons[i].Id, true);
            }

            return course;
        }

        [Fact]
        public async Task GetCourseProgress_PartialCompletion_ReturnsCorrectPercentage()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"prog-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"prog-student-{Guid.NewGuid():N}@test.local", "Student");

            int course = await SeedCourse(instructor, student, lessonCount: 3, completed: 1);

            var progress = await clsProgress.GetCourseProgress(student, course);

            Assert.Equal(3, progress.TotalLessons);
            Assert.Equal(1, progress.CompletedLessons);
            Assert.Equal(100.0 / 3, progress.ProgressPercentage, 2);
        }

        [Fact]
        public async Task GetCourseProgress_FullCompletion_Returns100()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"prog-full-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"prog-full-student-{Guid.NewGuid():N}@test.local", "Student");

            int course = await SeedCourse(instructor, student, lessonCount: 3, completed: 3);

            var progress = await clsProgress.GetCourseProgress(student, course);

            Assert.Equal(3, progress.TotalLessons);
            Assert.Equal(3, progress.CompletedLessons);
            Assert.Equal(100, progress.ProgressPercentage);
            Assert.All(progress.Lessons, l => Assert.True(l.IsComplete));
        }

        [Fact]
        public async Task GetCourseProgress_ZeroLessonCourse_Returns0Percent()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"prog-zero-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"prog-zero-student-{Guid.NewGuid():N}@test.local", "Student");

            int product = await _db.Seed.CourseProductAsync("Empty course product", 10m, instructor, true);
            int course = await _db.Seed.CourseAsync(product);
            await _db.Seed.EnrollDirectAsync(student, product);

            var progress = await clsProgress.GetCourseProgress(student, course);

            Assert.Equal(0, progress.TotalLessons);
            Assert.Equal(0, progress.CompletedLessons);
            Assert.Equal(0, progress.ProgressPercentage);
        }

        [Fact]
        public async Task IsCourseCompleted_TrueOnlyWhenAllLessonsComplete()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"prog-isc-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"prog-isc-student-{Guid.NewGuid():N}@test.local", "Student");

            int course = await SeedCourse(instructor, student, lessonCount: 2, completed: 1);

            Assert.False(await clsProgress.IsCourseComplated(student, course));

            // complete the remaining lesson
            var full = await EduCore_DataAccess.clsProgressData.GetCourseProgress(student, course);
            foreach (var lesson in full.Lessons)
                await _db.Seed.ProgressAsync(student, lesson.LessonId, true);

            Assert.True(await clsProgress.IsCourseComplated(student, course));
        }

        [Fact]
        public async Task IssueCertificate_IncompleteCourse_ThrowsConflict()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"cert-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"cert-student-{Guid.NewGuid():N}@test.local", "Student");

            int course = await SeedCourse(instructor, student, lessonCount: 2, completed: 1);

            await Assert.ThrowsAsync<ConflictException>(
                () => clsProgress.IssueCertificate(student, course));
        }

        [Fact]
        public async Task IssueCertificate_CompletedCourse_IssuesSameCodeTwice()
        {
            RequireDatabase();
            string storageDir = EnsureCertificateStorage();

            int instructor = await _db.Seed.UserAsync($"cert-full-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"cert-full-student-{Guid.NewGuid():N}@test.local", "Student");

            int course = await SeedCourse(instructor, student, lessonCount: 2, completed: 2);

            var first = await clsProgress.IssueCertificate(student, course);
            var second = await clsProgress.IssueCertificate(student, course);

            Assert.False(string.IsNullOrWhiteSpace(first.CertificateCode));
            Assert.Equal(first.CertificateCode, second.CertificateCode);

            // exactly one PDF exists for the certificate code
            string pdfPath = Path.Combine(storageDir, $"{first.CertificateCode}.pdf");
            Assert.True(File.Exists(pdfPath));
        }
    }
}
