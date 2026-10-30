using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace EduCore.IntegrationTests.TestSupport
{
    /// <summary>
    /// Creates known test data directly in the disposable EduCoreTests
    /// database. Every helper returns the new row id. Test fixtures live
    /// ONLY here — production code paths are never seeded from tests.
    /// </summary>
    public sealed class TestSeeder
    {
        private readonly DatabaseFixture _db;

        public TestSeeder(DatabaseFixture db) => _db = db;

        /// <summary>Default test password for every seeded account.</summary>
        public const string Password = "Test1234!";

        // ---------------- users / roles ----------------

        public async Task<int> UserAsync(string email, string role, bool isActive = true)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 4);

            int userId = await ScalarIntAsync(
                @"INSERT INTO Users (Name, BirthDate, Email, PasswordHash, RefreshTokenHash, RefreshTokenExpiresAt, IsActive)
                  VALUES (@Name, @BirthDate, @Email, @PasswordHash, @RefreshTokenHash, @RefreshTokenExpiresAt, @IsActive);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                c =>
                {
                    Bind(c, "@Name", SqlDbType.NVarChar, 150, "Test User " + email);
                    Bind(c, "@BirthDate", SqlDbType.Date, new DateTime(1995, 1, 1));
                    Bind(c, "@Email", SqlDbType.NVarChar, 254, email);
                    Bind(c, "@PasswordHash", SqlDbType.NVarChar, 255, hash);
                    Bind(c, "@RefreshTokenHash", SqlDbType.NVarChar, 255, BCrypt.Net.BCrypt.HashPassword("refresh-" + email, workFactor: 4));
                    Bind(c, "@RefreshTokenExpiresAt", SqlDbType.DateTime2, DateTime.UtcNow.AddDays(3));
                    Bind(c, "@IsActive", SqlDbType.Bit, isActive);
                });

            await RoleAsync(userId, role);
            return userId;
        }

        public Task RoleAsync(int userId, string role)
            => ExecAsync(
                @"INSERT INTO UserRoles (UserId, RoleId)
                  SELECT @UserId, RoleId FROM Roles WHERE Name = @Role;",
                c =>
                {
                    Bind(c, "@UserId", SqlDbType.Int, userId);
                    Bind(c, "@Role", SqlDbType.NVarChar, 50, role);
                });

        // ---------------- products ----------------

        public Task<int> ProductAsync(int productType, string name, decimal price, int createdBy, bool isPublished)
            => ScalarIntAsync(
                @"INSERT INTO Products (ProductType, Name, BasePrice, CreatedByUser, IsPublished)
                  VALUES (@Type, @Name, @Price, @CreatedByUser, @IsPublished);
                  SELECT SCOPE_IDENTITY();",
                c =>
                {
                    Bind(c, "@Type", SqlDbType.TinyInt, productType);
                    Bind(c, "@Name", SqlDbType.NVarChar, 120, name);
                    Bind(c, "@Price", SqlDbType.Decimal, price);
                    Bind(c, "@CreatedByUser", SqlDbType.Int, createdBy);
                    Bind(c, "@IsPublished", SqlDbType.Bit, isPublished);
                });

        public Task<int> LessonProductAsync(string name, decimal price, int createdBy, bool isPublished)
            => ProductAsync((int)Common.Enums.enProductType.Lesson, name, price, createdBy, isPublished);

        public Task<int> CourseProductAsync(string name, decimal price, int createdBy, bool isPublished)
            => ProductAsync((int)Common.Enums.enProductType.Course, name, price, createdBy, isPublished);

        public Task<int> BundleProductAsync(string name, decimal price, int createdBy, bool isPublished)
            => ProductAsync((int)Common.Enums.enProductType.Bundle, name, price, createdBy, isPublished);

        // ---------------- courses / lessons ----------------

        public Task<int> CourseAsync(int productId)
            => ScalarIntAsync(
                @"INSERT INTO Courses (ProductId) VALUES (@ProductId); SELECT SCOPE_IDENTITY();",
                c => Bind(c, "@ProductId", SqlDbType.Int, productId));

        public Task<int> LessonAsync(int productId, string title, int instructorId, int? courseId)
            => ScalarIntAsync(
                @"INSERT INTO Lessons (ProductId, Title, InstructorId, CourseId) VALUES (@ProductId, @Title, @InstructorId, @CourseId);
                  SELECT SCOPE_IDENTITY();",
                c =>
                {
                    Bind(c, "@ProductId", SqlDbType.Int, productId);
                    Bind(c, "@Title", SqlDbType.NVarChar, 150, title);
                    Bind(c, "@InstructorId", SqlDbType.Int, instructorId);
                    Bind(c, "@CourseId", SqlDbType.Int, (object?)courseId ?? DBNull.Value);
                });

        public Task LinkCourseLesson(int courseId, int lessonId)
            => ExecAsync(
                @"INSERT INTO CoursesLessons (CourseId, LessonId) VALUES (@CourseId, @LessonId);",
                c =>
                {
                    Bind(c, "@CourseId", SqlDbType.Int, courseId);
                    Bind(c, "@LessonId", SqlDbType.Int, lessonId);
                });

        public Task CourseInstructorAsync(int courseId, int instructorId)
            => ExecAsync(
                @"INSERT INTO CoursesInstructors (CourseId, InstructorId) VALUES (@CourseId, @InstructorId);",
                c =>
                {
                    Bind(c, "@CourseId", SqlDbType.Int, courseId);
                    Bind(c, "@InstructorId", SqlDbType.Int, instructorId);
                });

        // ---------------- bundles ----------------

        public Task<int> BundleAsync(int productId)
            => ScalarIntAsync(
                @"INSERT INTO Bundles (ProductId) VALUES (@ProductId); SELECT SCOPE_IDENTITY();",
                c => Bind(c, "@ProductId", SqlDbType.Int, productId));

        public Task AddBundleItemAsync(int bundleId, int courseId)
            => ExecAsync(
                @"INSERT INTO BundlesItems (BundleId, CourseId) VALUES (@BundleId, @CourseId);",
                c =>
                {
                    Bind(c, "@BundleId", SqlDbType.SmallInt, bundleId);
                    Bind(c, "@CourseId", SqlDbType.Int, courseId);
                });

        // ---------------- orders ----------------

        public Task<int> OrderAsync(int userId, string status, decimal total)
            => ScalarIntAsync(
                @"INSERT INTO Orders (UserId, TotalPrice, Status) VALUES (@UserId, @Total, @Status);
                  SELECT SCOPE_IDENTITY();",
                c =>
                {
                    Bind(c, "@UserId", SqlDbType.Int, userId);
                    Bind(c, "@Status", SqlDbType.VarChar, 50, status);
                    Bind(c, "@Total", SqlDbType.Decimal, total);
                });

        public Task<int> OrderItemAsync(int orderId, int productId, decimal price)
            => ScalarIntAsync(
                @"INSERT INTO OrderItems (OrderId, ProductId, PriceAtPurchase) VALUES (@OrderId, @ProductId, @Price);
                  SELECT SCOPE_IDENTITY();",
                c =>
                {
                    Bind(c, "@OrderId", SqlDbType.Int, orderId);
                    Bind(c, "@ProductId", SqlDbType.Int, productId);
                    Bind(c, "@Price", SqlDbType.Decimal, price);
                });

        // ---------------- payments ----------------

        public Task<int> PaymentAsync(int orderId, string idempotencyKey, string status, decimal price, short? discountId = null)
            => ScalarIntAsync(
                @"INSERT INTO Payments (OrderId, Price, DiscountId, DiscountPrice, PaymentMethod, Status, TransactionId, IdempotencyKey)
                  VALUES (@OrderId, @Price, @DiscountId, 0, 'card', @Status, @TransactionId, @IdempotencyKey);
                  SELECT SCOPE_IDENTITY();",
                c =>
                {
                    Bind(c, "@OrderId", SqlDbType.Int, orderId);
                    Bind(c, "@Price", SqlDbType.Decimal, price);
                    Bind(c, "@DiscountId", SqlDbType.SmallInt, (object?)discountId ?? DBNull.Value);
                    Bind(c, "@Status", SqlDbType.VarChar, 50, status);
                    Bind(c, "@TransactionId", SqlDbType.NVarChar, 200,
                        status == "Succeeded" ? $"tx-{orderId}" : DBNull.Value);
                    Bind(c, "@IdempotencyKey", SqlDbType.VarChar, 255, idempotencyKey);
                });

        /// <summary>
        /// Enrolls a user directly (order + succeeded payment + enrollment),
        /// satisfying the Enrollments→Payments foreign key.
        /// </summary>
        public async Task<int> EnrollDirectAsync(int userId, int productId, decimal price = 0m)
        {
            int orderId = await OrderAsync(userId, "Completed", price);
            int paymentId = await PaymentAsync(orderId, $"seed-{Guid.NewGuid():N}", "Succeeded", price);
            return await EnrollmentAsync(userId, productId, paymentId);
        }

        public Task<int> EnrollmentAsync(int userId, int productId, int paymentId)
            => ScalarIntAsync(
                @"INSERT INTO Enrollments (UserId, ProductId, PaymentId) VALUES (@UserId, @ProductId, @PaymentId);
                  SELECT SCOPE_IDENTITY();",
                c =>
                {
                    Bind(c, "@UserId", SqlDbType.Int, userId);
                    Bind(c, "@ProductId", SqlDbType.Int, productId);
                    Bind(c, "@PaymentId", SqlDbType.Int, paymentId);
                });

        public Task ProgressAsync(int userId, int lessonId, bool isComplete)
            => ExecAsync(
                @"IF NOT EXISTS (SELECT 1 FROM Progress WHERE UserId = @UserId AND LessonId = @LessonId)
                      INSERT INTO Progress (UserId, LessonId, IsComplete) VALUES (@UserId, @LessonId, @IsComplete);",
                c =>
                {
                    Bind(c, "@UserId", SqlDbType.Int, userId);
                    Bind(c, "@LessonId", SqlDbType.Int, lessonId);
                    Bind(c, "@IsComplete", SqlDbType.Bit, isComplete);
                });

        /// <summary>
        /// Seeds a course with N lessons (each with its own product row),
        /// marks the first `completedCount` lessons complete for the student,
        /// and enrolls the student in the course product.
        /// Returns (productId, courseId).
        /// </summary>
        public async Task<(int productId, int courseId)> CompletedCourseAsync(
            int instructorId, int studentId, int lessonCount, int completedCount)
        {
            int productId = await ProductAsync(
                (int)Common.Enums.enProductType.Course,
                $"Course {Guid.NewGuid():N}", 99m, instructorId, true);
            int courseId = await CourseAsync(productId);

            for (int i = 0; i < lessonCount; i++)
            {
                int lessonProductId = await ProductAsync(
                    (int)Common.Enums.enProductType.Lesson,
                    $"Lesson {i} of {Guid.NewGuid():N}", 0m, instructorId, true);
                int lessonId = await LessonAsync(lessonProductId, $"Lesson {i}", instructorId, courseId);
                await LinkCourseLesson(courseId, lessonId);
            }

            await EnrollDirectAsync(studentId, productId);

            var lessons = await EduCore_DataAccess.clsLessonsData.GetLessonsByCourse(courseId);
            for (int i = 0; i < Math.Min(completedCount, lessonCount); i++)
                await ProgressAsync(studentId, lessons[i].Id, true);

            return (productId, courseId);
        }

        // ---------------- assertion helpers ----------------

        public Task<string?> OrderStatusAsync(int orderId)
            => ScalarStringAsync(
                @"SELECT Status FROM Orders WHERE Id = @Id;",
                c => Bind(c, "@Id", SqlDbType.Int, orderId));

        public Task<string?> PaymentStatusAsync(int paymentId)
            => ScalarStringAsync(
                @"SELECT Status FROM Payments WHERE Id = @Id;",
                c => Bind(c, "@Id", SqlDbType.Int, paymentId));

        public Task<int> EnrollmentCountAsync(int userId, int productId)
            => ScalarIntAsync(
                @"SELECT COUNT(*) FROM Enrollments WHERE UserId = @UserId AND ProductId = @ProductId;",
                c =>
                {
                    Bind(c, "@UserId", SqlDbType.Int, userId);
                    Bind(c, "@ProductId", SqlDbType.Int, productId);
                });

        public Task<int?> DiscountUsageAsync(int discountId)
            => ScalarIntOrNullAsync(
                @"SELECT TotalUserNumber FROM DiscountCodes WHERE Id = @Id;",
                c => Bind(c, "@Id", SqlDbType.SmallInt, discountId));

        public Task<int> DiscountAsync(string code, int allowedUse, int createdById)
            => ScalarIntAsync(
                @"INSERT INTO DiscountCodes (DiscountCode, DiscountRate, CreatedById, AllowedUseNumber, TotalUserNumber)
                  VALUES (@Code, @Rate, @CreatedBy, @Allowed, 0); SELECT SCOPE_IDENTITY();",
                c =>
                {
                    Bind(c, "@Code", SqlDbType.NVarChar, 200, code);
                    Bind(c, "@Rate", SqlDbType.Decimal, 50m);
                    Bind(c, "@CreatedBy", SqlDbType.Int, 1);
                    Bind(c, "@Allowed", SqlDbType.SmallInt, allowedUse);
                });

        // ---------------- low-level helpers ----------------

        private static void Bind(SqlCommand command, string name, SqlDbType type, object value)
            => command.Parameters.Add(name, type).Value = value ?? DBNull.Value;

        private static void Bind(SqlCommand command, string name, SqlDbType type, int size, object value)
            => command.Parameters.Add(name, type, size).Value = value ?? DBNull.Value;

        private async Task<int> ScalarIntAsync(string sql, Action<SqlCommand> bind)
        {
            await using SqlConnection con = new(_db.TestDatabaseConnectionString);
            await con.OpenAsync();
            await using SqlCommand command = new(sql, con);
            bind(command);
            object result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task<int?> ScalarIntOrNullAsync(string sql, Action<SqlCommand> bind)
        {
            await using SqlConnection con = new(_db.TestDatabaseConnectionString);
            await con.OpenAsync();
            await using SqlCommand command = new(sql, con);
            bind(command);
            object result = await command.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        private async Task<string?> ScalarStringAsync(string sql, Action<SqlCommand> bind)
        {
            await using SqlConnection con = new(_db.TestDatabaseConnectionString);
            await con.OpenAsync();
            await using SqlCommand command = new(sql, con);
            bind(command);
            object result = await command.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : Convert.ToString(result);
        }

        private async Task ExecAsync(string sql, Action<SqlCommand> bind)
        {
            await using SqlConnection con = new(_db.TestDatabaseConnectionString);
            await con.OpenAsync();
            await using SqlCommand command = new(sql, con);
            bind(command);
            await command.ExecuteNonQueryAsync();
        }
    }
}
