using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Dtos;
using EduCore.IntegrationTests.TestSupport;
using EduCore_DataAccess;
using Xunit;

namespace EduCore.IntegrationTests
{
    [Collection(DatabaseCollection.Name)]
    public class ProgressConcurrencyTests
    {
        private readonly DatabaseFixture _db;

        public ProgressConcurrencyTests(DatabaseFixture db) => _db = db;

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        [Fact]
        public async Task ConcurrentInsertion_SingleRow_NoFailures()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pg-u-{Guid.NewGuid():N}@test.local", "Student");
            int creator = await _db.Seed.UserAsync($"pg-c-{Guid.NewGuid():N}@test.local", "Instructor");
            int lessonProduct = await _db.Seed.LessonProductAsync($"PG lesson {Guid.NewGuid():N}", 0m, creator, true);
            int lessonId = await _db.Seed.LessonAsync(lessonProduct, "Lesson", creator, null);

            int tasksCount = 8;
            var results = new bool[tasksCount];
            var exceptions = new Exception[tasksCount];

            await Task.WhenAll(Enumerable.Range(0, tasksCount).Select(i => Task.Run(async () =>
            {
                try
                {
                    results[i] = await clsProgressData.UpsertProgress(new DtoProgress
                    {
                        UserId = user,
                        LessonId = lessonId,
                        IsComplete = (i % 2 == 0)
                    });
                }
                catch (Exception ex)
                {
                    exceptions[i] = ex;
                }
            })).ToArray());

            Assert.All(exceptions, e => Assert.Null(e));
            int rowCount = await _db.Seed.ScalarIntRawAsync(
                "SELECT COUNT(*) FROM Progress WHERE UserId = @U AND LessonId = @L;",
                c =>
                {
                    c.Parameters.Add("@U", System.Data.SqlDbType.Int).Value = user;
                    c.Parameters.Add("@L", System.Data.SqlDbType.Int).Value = lessonId;
                });
            Assert.Equal(1, rowCount);
        }

        [Fact]
        public async Task ConcurrentUpdates_SingleRow_ValidFinalState()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pgu-u-{Guid.NewGuid():N}@test.local", "Student");
            int creator = await _db.Seed.UserAsync($"pgu-c-{Guid.NewGuid():N}@test.local", "Instructor");
            int lessonProduct = await _db.Seed.LessonProductAsync($"PGU lesson {Guid.NewGuid():N}", 0m, creator, true);
            int lessonId = await _db.Seed.LessonAsync(lessonProduct, "Lesson", creator, null);

            // seed existing row
            await clsProgressData.UpsertProgress(new DtoProgress { UserId = user, LessonId = lessonId, IsComplete = false });

            int tasksCount = 8;
            var exceptions = new Exception[tasksCount];
            await Task.WhenAll(Enumerable.Range(0, tasksCount).Select(i => Task.Run(async () =>
            {
                try
                {
                    await clsProgressData.UpsertProgress(new DtoProgress
                    {
                        UserId = user,
                        LessonId = lessonId,
                        IsComplete = (i % 2 == 0)
                    });
                }
                catch (Exception ex)
                {
                    exceptions[i] = ex;
                }
            })).ToArray());

            Assert.All(exceptions, e => Assert.Null(e));
            int rowCount = await _db.Seed.ScalarIntRawAsync(
                "SELECT COUNT(*) FROM Progress WHERE UserId = @U AND LessonId = @L;",
                c =>
                {
                    c.Parameters.Add("@U", System.Data.SqlDbType.Int).Value = user;
                    c.Parameters.Add("@L", System.Data.SqlDbType.Int).Value = lessonId;
                });
            Assert.Equal(1, rowCount);

            var dto = await clsProgressData.GetProgress(user, lessonId);
            Assert.NotNull(dto);
            Assert.True(dto.IsComplete.HasValue);
        }

        [Fact]
        public async Task CompletionDate_SetOnComplete_PreservedOnIncomplete()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pgd-u-{Guid.NewGuid():N}@test.local", "Student");
            int creator = await _db.Seed.UserAsync($"pgd-c-{Guid.NewGuid():N}@test.local", "Instructor");
            int lessonProduct = await _db.Seed.LessonProductAsync($"PGD lesson {Guid.NewGuid():N}", 0m, creator, true);
            int lessonId = await _db.Seed.LessonAsync(lessonProduct, "Lesson", creator, null);

            Assert.True(await clsProgressData.UpsertProgress(new DtoProgress { UserId = user, LessonId = lessonId, IsComplete = true }));
            var afterComplete = await clsProgressData.GetProgress(user, lessonId);
            Assert.True(afterComplete.IsComplete);
            Assert.NotNull(afterComplete.CompletedDate);

            Assert.True(await clsProgressData.UpsertProgress(new DtoProgress { UserId = user, LessonId = lessonId, IsComplete = false }));
            var afterIncomplete = await clsProgressData.GetProgress(user, lessonId);
            Assert.False(afterIncomplete.IsComplete ?? true);
            // CompletedDate preserved per existing business rule
            Assert.NotNull(afterIncomplete.CompletedDate);
        }
    }
}