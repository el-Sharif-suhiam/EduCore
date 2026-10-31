using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;
using Common.Exceptions;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using EduCore_DataAccess;
using Microsoft.Data.SqlClient;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Concurrency regression tests for payment/discount reservation.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class PaymentConcurrencyTests
    {
        private readonly DatabaseFixture _db;

        public PaymentConcurrencyTests(DatabaseFixture db) => _db = db;

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private async Task<(int userA, int userB, int product, int discountId, int orderA, int orderB, int paymentAId)> SeedAsync(int allowedUse)
        {
            int userA = await _db.Seed.UserAsync($"pc-a-{Guid.NewGuid():N}@test.local", "Student");
            int userB = await _db.Seed.UserAsync($"pc-b-{Guid.NewGuid():N}@test.local", "Student");
            int creator = await _db.Seed.UserAsync($"pc-c-{Guid.NewGuid():N}@test.local", "Instructor");
            int product = await _db.Seed.LessonProductAsync($"PC prod {Guid.NewGuid():N}", 30m, creator, true);

            string code = $"PC{Guid.NewGuid():N}"[..12];
            int discountId = await _db.Seed.DiscountAsync(code, allowedUse, creator);

            int orderA = await _db.Seed.OrderAsync(userA, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderA, product, 30m);
            int paymentAId = await _db.Seed.PaymentAsync(orderA, $"idem-a-{Guid.NewGuid():N}", "Pending", 30m, (short)discountId);

            int orderB = await _db.Seed.OrderAsync(userB, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderB, product, 30m);

            return (userA, userB, product, discountId, orderA, orderB, paymentAId);
        }

        [Fact]
        public async Task CompetingReservations_FinalSlot_OnlyOneSucceeds()
        {
            RequireDatabase();

            int userA = await _db.Seed.UserAsync($"cr-a-{Guid.NewGuid():N}@test.local", "Student");
            int userB = await _db.Seed.UserAsync($"cr-b-{Guid.NewGuid():N}@test.local", "Student");
            int creator = await _db.Seed.UserAsync($"cr-c-{Guid.NewGuid():N}@test.local", "Instructor");
            int product = await _db.Seed.LessonProductAsync($"CR prod {Guid.NewGuid():N}", 30m, creator, true);

            string code = $"CR{Guid.NewGuid():N}"[..12];
            int discountId = await _db.Seed.DiscountAsync(code, allowedUse: 1, creator);

            int orderA = await _db.Seed.OrderAsync(userA, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderA, product, 30m);
            int orderB = await _db.Seed.OrderAsync(userB, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderB, product, 30m);

            int successes = 0;
            int rejections = 0;
            int deadlocks = 0;
            Exception unexpected = null;
            SqlException rejectionEx = null;

            Func<int, Task> attempt = async (orderId) =>
            {
                try
                {
                    var p = new clsPayment();
                    p.SetPaymentMethod("card");
                    bool r = await p.CreatePayment(orderId, (short)discountId, $"idem-{Guid.NewGuid():N}");
                    if (r) successes++;
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 1205) { deadlocks++; unexpected = ex; }
                    else if (ex.Number == 50004) { rejections++; rejectionEx = ex; }
                    else { unexpected = ex; }
                }
                catch (Exception ex)
                {
                    unexpected = ex;
                }
            };

            await Task.WhenAll(attempt(orderA), attempt(orderB));

            Assert.Equal(0, deadlocks);
            Assert.Null(unexpected);
            Assert.Equal(1, successes);
            Assert.Equal(1, rejections);
            Assert.NotNull(rejectionEx);

            // exactly one pending payment created for this discount
            int pendingCount = await _db.Seed.ScalarIntRawAsync(
                "SELECT COUNT(*) FROM Payments WHERE DiscountId = @D AND Status = 'Pending';",
                c => c.Parameters.Add("@D", System.Data.SqlDbType.SmallInt).Value = discountId);
            Assert.Equal(1, pendingCount);

            // no usage increment yet (TotalUserNumber counts successes)
            Assert.Equal(0, await _db.Seed.DiscountUsageAsync(discountId));
        }

        [Fact]
        public async Task PaymentCompletion_Versus_NewReservation_NoDeadlock()
        {
            RequireDatabase();

            int userA = await _db.Seed.UserAsync($"pc2-a-{Guid.NewGuid():N}@test.local", "Student");
            int userB = await _db.Seed.UserAsync($"pc2-b-{Guid.NewGuid():N}@test.local", "Student");
            int creator = await _db.Seed.UserAsync($"pc2-c-{Guid.NewGuid():N}@test.local", "Instructor");
            int product = await _db.Seed.LessonProductAsync($"PC2 prod {Guid.NewGuid():N}", 30m, creator, true);

            string code = $"P2{Guid.NewGuid():N}"[..12];
            int allowedUse = 1;
            int discountId = await _db.Seed.DiscountAsync(code, allowedUse, creator);

            int orderA = await _db.Seed.OrderAsync(userA, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderA, product, 30m);
            int paymentAId = await _db.Seed.PaymentAsync(orderA, $"idem-pa-{Guid.NewGuid():N}", "Pending", 30m, (short)discountId);

            int orderB = await _db.Seed.OrderAsync(userB, "Pending", 30m);
            await _db.Seed.OrderItemAsync(orderB, product, 30m);

            int deadlocks = 0;
            bool completionOk = false;
            bool creationSucceeded = false;
            Exception completionEx = null;
            Exception creationEx = null;

            // Synchronize so both operations start together and genuinely overlap.
            var barrier = new System.Threading.Barrier(2);

            var t1 = Task.Run(async () =>
            {
                barrier.SignalAndWait();
                try
                {
                    var payment = await clsPayment.FindAsync(paymentAId);
                    completionOk = await clsCheckoutService.CompletePaymentAsync(userA, payment, $"tx-{Guid.NewGuid():N}");
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 1205) deadlocks++;
                    completionEx = ex;
                }
            });

            var t2 = Task.Run(async () =>
            {
                barrier.SignalAndWait();
                try
                {
                    var p = new clsPayment();
                    p.SetPaymentMethod("card");
                    creationSucceeded = await p.CreatePayment(orderB, (short)discountId, $"idem-pb-{Guid.NewGuid():N}");
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 1205) deadlocks++;
                    creationEx = ex;
                }
            });

            await Task.WhenAll(t1, t2);

            // A deadlock must never occur.
            Assert.Equal(0, deadlocks);
            Assert.Null(completionEx);
            Assert.True(completionOk, "payment completion must succeed");

            // The second reservation must be rejected specifically because the
            // discount usage limit has been reached (SP_CreateNewPayment throws
            // error 50004 'Discount limit reached').
            Assert.False(creationSucceeded, "second reservation must not succeed");
            var creationSqlEx = Assert.IsType<SqlException>(creationEx);
            Assert.Equal(50004, creationSqlEx.Number);

            // payment A succeeded
            Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentAId));
            Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderA));

            // No second pending payment was created for this discount.
            int pending = await _db.Seed.ScalarIntRawAsync(
                "SELECT COUNT(*) FROM Payments WHERE DiscountId = @D AND Status = 'Pending';",
                c => c.Parameters.Add("@D", System.Data.SqlDbType.SmallInt).Value = discountId);
            Assert.Equal(0, pending);

            // Invariant: successful usage + pending reservations <= AllowedUseNumber.
            int usage = (await _db.Seed.DiscountUsageAsync(discountId)) ?? 0;
            Assert.True(usage + pending <= allowedUse,
                $"usage({usage}) + pending({pending}) exceeded allowed({allowedUse})");
        }

        [Fact]
        public async Task PaymentCompletion_Versus_NewReservation_Repeated_NoDeadlock()
        {
            RequireDatabase();

            for (int i = 0; i < 10; i++)
            {
                int userA = await _db.Seed.UserAsync($"pcr-a-{i}-{Guid.NewGuid():N}@test.local", "Student");
                int userB = await _db.Seed.UserAsync($"pcr-b-{i}-{Guid.NewGuid():N}@test.local", "Student");
                int creator = await _db.Seed.UserAsync($"pcr-c-{i}-{Guid.NewGuid():N}@test.local", "Instructor");
                int product = await _db.Seed.LessonProductAsync($"PCR prod {i} {Guid.NewGuid():N}", 30m, creator, true);

                string code = $"PR{i}{Guid.NewGuid():N}"[..12];
                int discountId = await _db.Seed.DiscountAsync(code, allowedUse: 1, creator);

                int orderA = await _db.Seed.OrderAsync(userA, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderA, product, 30m);
                int paymentAId = await _db.Seed.PaymentAsync(orderA, $"idem-pa-{Guid.NewGuid():N}", "Pending", 30m, (short)discountId);

                int orderB = await _db.Seed.OrderAsync(userB, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderB, product, 30m);

                int deadlocks = 0;
                Exception completionEx = null;
                Exception creationEx = null;
                bool completionOk = false;
                bool creationSucceeded = false;

                var barrier = new System.Threading.Barrier(2);
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        barrier.SignalAndWait();
                        try
                        {
                            var payment = await clsPayment.FindAsync(paymentAId);
                            completionOk = await clsCheckoutService.CompletePaymentAsync(userA, payment, $"tx-{Guid.NewGuid():N}");
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 1205) deadlocks++;
                            completionEx = ex;
                        }
                    }),
                    Task.Run(async () =>
                    {
                        barrier.SignalAndWait();
                        try
                        {
                            var p = new clsPayment(); p.SetPaymentMethod("card");
                            creationSucceeded = await p.CreatePayment(orderB, (short)discountId, $"idem-pb-{Guid.NewGuid():N}");
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 1205) deadlocks++;
                            creationEx = ex;
                        }
                    })
                );

                Assert.Equal(0, deadlocks);
                Assert.Null(completionEx);
                Assert.True(completionOk);
                Assert.False(creationSucceeded);
                var creationSqlEx = Assert.IsType<SqlException>(creationEx);
                Assert.Equal(50004, creationSqlEx.Number);

                Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentAId));
                Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderA));

                int pending = await _db.Seed.ScalarIntRawAsync(
                    "SELECT COUNT(*) FROM Payments WHERE DiscountId = @D AND Status = 'Pending';",
                    c => c.Parameters.Add("@D", System.Data.SqlDbType.SmallInt).Value = discountId);
                Assert.Equal(0, pending);

                int usage = (await _db.Seed.DiscountUsageAsync(discountId)) ?? 0;
                Assert.True(usage + pending <= 1, $"usage({usage}) + pending({pending}) exceeded 1");
            }

            // If we reach here, all iterations passed.
        }

        [Fact]
        public async Task CompetingReservations_Repeated_NoDeadlock()
        {
            RequireDatabase();

            int totalDeadlocks = 0;
            for (int i = 0; i < 15; i++)
            {
                int userA = await _db.Seed.UserAsync($"rep-a-{i}-{Guid.NewGuid():N}@test.local", "Student");
                int userB = await _db.Seed.UserAsync($"rep-b-{i}-{Guid.NewGuid():N}@test.local", "Student");
                int creator = await _db.Seed.UserAsync($"rep-c-{i}-{Guid.NewGuid():N}@test.local", "Instructor");
                int product = await _db.Seed.LessonProductAsync($"REP prod {i} {Guid.NewGuid():N}", 30m, creator, true);

                string code = $"RP{i}{Guid.NewGuid():N}"[..12];
                int discountId = await _db.Seed.DiscountAsync(code, allowedUse: 1, creator);

                int orderA = await _db.Seed.OrderAsync(userA, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderA, product, 30m);
                int orderB = await _db.Seed.OrderAsync(userB, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderB, product, 30m);

                int deadlocks = 0;
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        try
                        {
                            var p = new clsPayment(); p.SetPaymentMethod("card");
                            await p.CreatePayment(orderA, (short)discountId, $"idem-{Guid.NewGuid():N}");
                        }
                        catch (SqlException ex) { if (ex.Number == 1205) deadlocks++; }
                        catch { }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            var p = new clsPayment(); p.SetPaymentMethod("card");
                            await p.CreatePayment(orderB, (short)discountId, $"idem-{Guid.NewGuid():N}");
                        }
                        catch (SqlException ex) { if (ex.Number == 1205) deadlocks++; }
                        catch { }
                    })
                );
            }

            Assert.Equal(0, totalDeadlocks);
        }
    }
}