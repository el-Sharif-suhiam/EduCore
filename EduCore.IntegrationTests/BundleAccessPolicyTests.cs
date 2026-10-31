using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using EduCore_DataAccess;
using Xunit;

namespace EduCore.IntegrationTests
{
    [Collection(DatabaseCollection.Name)]
    public class BundleAccessPolicyTests
    {
        private readonly DatabaseFixture _db;

        public BundleAccessPolicyTests(DatabaseFixture db) => _db = db;

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private async Task ExecSqlAsync(string sql, Action<SqlCommand> bind)
        {
            await using SqlConnection con = new(_db.TestDatabaseConnectionString);
            await con.OpenAsync();
            await using SqlCommand cmd = new(sql, con);
            bind(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task PublishedBundle_PublishedCourse_AppearsInPublicDetails()
        {
            RequireDatabase();
            int owner = await _db.Seed.UserAsync($"bap-o-{Guid.NewGuid():N}@test.local", "Instructor");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, owner, true);
            int course = await _db.Seed.CourseAsync(courseProduct);
            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, owner, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);

            var vm = await clsBundleItemsData.GetBundleWithCourses((short)bundle);
            Assert.NotNull(vm.Courses);
            Assert.Contains(vm.Courses, c => c.CourseId == course);
        }

        [Fact]
        public async Task PublishedBundle_UnpublishedCourse_ExcludedFromPublicDetails()
        {
            RequireDatabase();
            int owner = await _db.Seed.UserAsync($"bap-u-{Guid.NewGuid():N}@test.local", "Instructor");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, owner, false); // unpublished
            int course = await _db.Seed.CourseAsync(courseProduct);
            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, owner, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);

            var vm = await clsBundleItemsData.GetBundleWithCourses((short)bundle);
            // either empty courses or not containing the unpublished course
            if (vm.Courses != null)
                Assert.DoesNotContain(vm.Courses, c => c.CourseId == course);
        }

        [Fact]
        public async Task PublishedBundle_DeletedCourse_ExcludedFromPublicDetails()
        {
            RequireDatabase();
            int owner = await _db.Seed.UserAsync($"bap-d-{Guid.NewGuid():N}@test.local", "Instructor");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, owner, true);
            int course = await _db.Seed.CourseAsync(courseProduct);
            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, owner, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);

            // soft-delete the course
            await ExecSqlAsync("UPDATE Courses SET IsDeleted = 1, DeletedAt = GETDATE() WHERE Id = @Id;",
                c => c.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = course);

            var vm = await clsBundleItemsData.GetBundleWithCourses((short)bundle);
            if (vm.Courses != null)
                Assert.DoesNotContain(vm.Courses, c => c.CourseId == course);
        }

        [Fact]
        public async Task UnpublishedBundle_NotVisiblePublicly()
        {
            RequireDatabase();
            int owner = await _db.Seed.UserAsync($"bap-ub-{Guid.NewGuid():N}@test.local", "Instructor");
            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, owner, false);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);

            var vm = await clsBundleItemsData.GetBundleWithCourses((short)bundle);
            // unpublished bundle product -> query filters BP.IsPublished=1 -> no rows -> empty
            Assert.True(vm.Courses == null || vm.Courses.Count == 0);
        }

        [Fact]
        public async Task ActiveBundlePurchaser_UnpublishedNonDeletedCourse_Granted()
        {
            RequireDatabase();
            int instructor = await _db.Seed.UserAsync($"bap-ap-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"bap-as-{Guid.NewGuid():N}@test.local", "Student");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, false); // unpublished
            int course = await _db.Seed.CourseAsync(courseProduct);
            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, instructor, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);
            await _db.Seed.EnrollDirectAsync(student, bundleProduct);

            Assert.True(await clsEnrollment.IsUserEnrolled(student, courseProduct));
        }

        [Fact]
        public async Task BundlePurchaser_DeletedCourse_Denied()
        {
            RequireDatabase();
            int instructor = await _db.Seed.UserAsync($"bap-bd-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"bap-bs-{Guid.NewGuid():N}@test.local", "Student");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, true);
            int course = await _db.Seed.CourseAsync(courseProduct);
            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, instructor, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);
            await _db.Seed.EnrollDirectAsync(student, bundleProduct);

            await ExecSqlAsync("UPDATE Courses SET IsDeleted = 1 WHERE Id = @Id;",
                c => c.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = course);

            Assert.False(await clsEnrollment.IsUserEnrolled(student, courseProduct));
        }

        [Fact]
        public async Task ExpiredBundleEnrollment_Denied()
        {
            RequireDatabase();
            int instructor = await _db.Seed.UserAsync($"bap-be-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"bap-bes-{Guid.NewGuid():N}@test.local", "Student");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, true);
            int course = await _db.Seed.CourseAsync(courseProduct);
            int bundleProduct = await _db.Seed.BundleProductAsync($"Bundle {Guid.NewGuid():N}", 80m, instructor, true);
            int bundle = await _db.Seed.BundleAsync(bundleProduct);
            await _db.Seed.AddBundleItemAsync(bundle, course);
            await _db.Seed.EnrollDirectAsync(student, bundleProduct, expireAt: DateTime.UtcNow.Date.AddDays(-1));

            Assert.False(await clsEnrollment.IsUserEnrolled(student, courseProduct));
        }

        [Fact]
        public async Task BundleEnrollment_DoesNotGrantUnrelatedCourse()
        {
            RequireDatabase();
            int instructor = await _db.Seed.UserAsync($"bap-unr-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"bap-unrs-{Guid.NewGuid():N}@test.local", "Student");
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
        public async Task DirectEnrollment_DeletedCourse_Denied()
        {
            RequireDatabase();
            int instructor = await _db.Seed.UserAsync($"bap-de-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"bap-des-{Guid.NewGuid():N}@test.local", "Student");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, true);
            int course = await _db.Seed.CourseAsync(courseProduct);
            await _db.Seed.EnrollDirectAsync(student, courseProduct);

            await ExecSqlAsync("UPDATE Courses SET IsDeleted = 1 WHERE Id = @Id;",
                c => c.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = course);

            Assert.False(await clsEnrollment.IsUserEnrolled(student, courseProduct));
        }

        [Fact]
        public async Task DirectEnrollment_UnpublishedCourse_StillGranted()
        {
            RequireDatabase();
            int instructor = await _db.Seed.UserAsync($"bap-du-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"bap-dus-{Guid.NewGuid():N}@test.local", "Student");
            int courseProduct = await _db.Seed.CourseProductAsync($"Course {Guid.NewGuid():N}", 50m, instructor, false); // unpublished
            await _db.Seed.CourseAsync(courseProduct);
            await _db.Seed.EnrollDirectAsync(student, courseProduct);

            Assert.True(await clsEnrollment.IsUserEnrolled(student, courseProduct));
        }
    }
}