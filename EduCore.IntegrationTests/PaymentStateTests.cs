using System;
using System.Threading.Tasks;
using Common.Enums;
using Common.Exceptions;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Regression tests for Issue F: payment status transitions must be
    /// conditional on Pending, and completing the same payment twice must
    /// not duplicate business effects (discount usage, enrollments).
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class PaymentStateTests
    {
        private readonly DatabaseFixture _db;

        public PaymentStateTests(DatabaseFixture db) => _db = db;

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private async Task<int> ProductAsync(int createdBy, bool published = true)
            => await _db.Seed.LessonProductAsync($"Pay prod {Guid.NewGuid():N}", 30m, createdBy, published);

        [Fact]
        public async Task MarkAsFailed_SucceededPayment_ThrowsConflict()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pay-f-{Guid.NewGuid():N}@test.local", "Student");
            int product = await ProductAsync(user);
            int orderId = await _db.Seed.OrderAsync(user, "Pending", 30m);
            int paymentId = await _db.Seed.PaymentAsync(orderId, $"idem-{Guid.NewGuid():N}", "Succeeded", 30m);

            var payment = await clsPayment.FindAsync(paymentId);

            await Assert.ThrowsAsync<ConflictException>(
                () => payment.MarkAsFailedAsync(user));

            Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentId));
        }

        [Fact]
        public async Task MarkAsExpired_SucceededPayment_ThrowsConflict()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pay-e-{Guid.NewGuid():N}@test.local", "Student");
            int product = await ProductAsync(user);
            int orderId = await _db.Seed.OrderAsync(user, "Pending", 30m);
            int paymentId = await _db.Seed.PaymentAsync(orderId, $"idem-{Guid.NewGuid():N}", "Succeeded", 30m);

            var payment = await clsPayment.FindAsync(paymentId);

            await Assert.ThrowsAsync<ConflictException>(
                () => payment.MarkAsExpiredAsync(user));

            Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentId));
        }

        [Fact]
        public async Task MarkAsFailed_PendingPayment_Succeeds()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pay-p-{Guid.NewGuid():N}@test.local", "Student");
            int product = await ProductAsync(user);
            int orderId = await _db.Seed.OrderAsync(user, "Pending", 30m);
            int paymentId = await _db.Seed.PaymentAsync(orderId, $"idem-{Guid.NewGuid():N}", "Pending", 30m);

            var payment = await clsPayment.FindAsync(paymentId);
            bool result = await payment.MarkAsFailedAsync(user, "tx-failed");

            Assert.True(result);
            Assert.Equal("Failed", await _db.Seed.PaymentStatusAsync(paymentId));
        }

        [Fact]
        public async Task CompletePaymentAsync_AlreadySucceededPayment_IsRejected()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pay-d-{Guid.NewGuid():N}@test.local", "Student");
            int product = await ProductAsync(user);
            int orderId = await _db.Seed.OrderAsync(user, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderId, product, 30m);
            int paymentId = await _db.Seed.PaymentAsync(orderId, $"idem-{Guid.NewGuid():N}", "Succeeded", 30m);

            var payment = await clsPayment.FindAsync(paymentId);

            // A replayed completion (admin hook after webhook, or duplicate
            // call) must NOT re-run the side-effect chain.
            await Assert.ThrowsAsync<ConflictException>(
                () => clsCheckoutService.CompletePaymentAsync(user, payment, "tx-replay"));

            // no new effects: the replay must not create enrollments
            Assert.Equal(0, await _db.Seed.EnrollmentCountAsync(user, product));
            Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentId));
        }

        [Fact]
        public async Task CompletePaymentAsync_PendingPayment_CompletesAtomically()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pay-s-{Guid.NewGuid():N}@test.local", "Student");
            int product = await ProductAsync(user);
            int discountId = await _db.Seed.DiscountAsync($"SAVE{Guid.NewGuid():N}"[..10], allowedUse: 10, createdById: user);

            int orderId = await _db.Seed.OrderAsync(user, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderId, product, 30m);
            int paymentId = await _db.Seed.PaymentAsync(orderId, $"idem-{Guid.NewGuid():N}", "Pending", 30m, (short)discountId);

            var payment = await clsPayment.FindAsync(paymentId);
            bool result = await clsCheckoutService.CompletePaymentAsync(user, payment, "tx-success-1");

            Assert.True(result);
            Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentId));
            Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderId));
            Assert.Equal(1, await _db.Seed.EnrollmentCountAsync(user, product));
            Assert.Equal(1, await _db.Seed.DiscountUsageAsync(discountId));
        }

        [Fact]
        public async Task CreatePayment_SameIdempotencyKey_ReturnsExistingPayment()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"pay-i-{Guid.NewGuid():N}@test.local", "Student");
            int product = await ProductAsync(user);
            int orderId = await _db.Seed.OrderAsync(user, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderId, product, 30m);

            string key = $"idem-{Guid.NewGuid():N}";

            var first = new clsPayment();
            first.SetPaymentMethod("card");
            bool firstResult = await first.CreatePayment(orderId, null, key);

            var second = new clsPayment();
            second.SetPaymentMethod("card");
            bool secondResult = await second.CreatePayment(orderId, null, key);

            Assert.True(firstResult);
            Assert.True(secondResult);
            Assert.Equal(first.Id, second.Id);
        }
    }
}
