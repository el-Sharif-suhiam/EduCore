using System;
using System.Linq;
using System.Threading;
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

        private sealed class AttemptResult
        {
            public bool Success { get; set; }
            public int? SqlErrorNumber { get; set; }
            public Exception? Exception { get; set; }
            public bool TimedOutOnBarrier { get; set; }
        }

        private sealed class OperationResult
        {
            public bool Success { get; set; }
            public int? SqlErrorNumber { get; set; }
            public Exception? Exception { get; set; }
            public bool TimedOutOnBarrier { get; set; }
        }

        private static readonly TimeSpan BarrierTimeout = TimeSpan.FromSeconds(10);

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

            using var barrier = new Barrier(2);

            Func<int, Task<AttemptResult>> attempt = (orderId) => Task.Run(async () =>
            {
                var result = new AttemptResult();
                try
                {
                    if (!barrier.SignalAndWait(BarrierTimeout))
                    {
                        result.TimedOutOnBarrier = true;
                        result.Exception = new TimeoutException(
                            $"Concurrency barrier timed out after {BarrierTimeout.TotalSeconds} seconds while waiting for both reservation attempts to start.");
                        return result;
                    }

                    var p = new clsPayment();
                    p.SetPaymentMethod("card");
                    result.Success = await p.CreatePayment(orderId, (short)discountId, $"idem-{Guid.NewGuid():N}");
                }
                catch (SqlException ex)
                {
                    result.SqlErrorNumber = ex.Number;
                    result.Exception = ex;
                }
                catch (Exception ex)
                {
                    result.Exception = ex;
                }

                return result;
            });

            AttemptResult[] results = await Task.WhenAll(attempt(orderA), attempt(orderB));

            int successes = results.Count(r => r.Success);
            int rejections = results.Count(r => r.SqlErrorNumber == 50004);
            int deadlocks = results.Count(r => r.SqlErrorNumber == 1205);
            AttemptResult? unexpected = results.FirstOrDefault(r =>
                r.Exception is not null &&
                !(r.Exception is SqlException sql && sql.Number == 50004));

            Assert.Equal(0, deadlocks);
            Assert.Null(unexpected?.Exception);
            Assert.Equal(1, successes);
            Assert.Equal(1, rejections);

            foreach (AttemptResult result in results)
            {
                if (result.Success)
                {
                    Assert.Null(result.Exception);
                }
                else
                {
                    SqlException sqlException = Assert.IsType<SqlException>(result.Exception);
                    Assert.Equal(50004, sqlException.Number);
                }
            }

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

            using var barrier = new Barrier(2);

            var completion = new OperationResult();
            var creation = new OperationResult();

            Task t1 = Task.Run(async () =>
            {
                try
                {
                    if (!barrier.SignalAndWait(BarrierTimeout))
                    {
                        completion.TimedOutOnBarrier = true;
                        completion.Exception = new TimeoutException(
                            $"Concurrency barrier timed out after {BarrierTimeout.TotalSeconds} seconds while waiting for payment completion and reservation creation to start.");
                        return;
                    }

                    var payment = await clsPayment.FindAsync(paymentAId);
                    completion.Success = await clsCheckoutService.CompletePaymentAsync(userA, payment, $"tx-{Guid.NewGuid():N}");
                }
                catch (SqlException ex)
                {
                    completion.SqlErrorNumber = ex.Number;
                    completion.Exception = ex;
                }
                catch (Exception ex)
                {
                    completion.Exception = ex;
                }
            });

            Task t2 = Task.Run(async () =>
            {
                try
                {
                    if (!barrier.SignalAndWait(BarrierTimeout))
                    {
                        creation.TimedOutOnBarrier = true;
                        creation.Exception = new TimeoutException(
                            $"Concurrency barrier timed out after {BarrierTimeout.TotalSeconds} seconds while waiting for payment completion and reservation creation to start.");
                        return;
                    }

                    var p = new clsPayment();
                    p.SetPaymentMethod("card");
                    creation.Success = await p.CreatePayment(orderB, (short)discountId, $"idem-pb-{Guid.NewGuid():N}");
                }
                catch (SqlException ex)
                {
                    creation.SqlErrorNumber = ex.Number;
                    creation.Exception = ex;
                }
                catch (Exception ex)
                {
                    creation.Exception = ex;
                }
            });

            await Task.WhenAll(t1, t2);

            Assert.False(completion.TimedOutOnBarrier, completion.Exception?.Message);
            Assert.False(creation.TimedOutOnBarrier, creation.Exception?.Message);
            Assert.Null(completion.Exception);
            Assert.True(completion.Success, "payment completion must succeed");
            Assert.NotEqual(1205, completion.SqlErrorNumber);

            Assert.False(creation.Success, "second reservation must not succeed");
            SqlException creationSqlException = Assert.IsType<SqlException>(creation.Exception);
            Assert.Equal(50004, creationSqlException.Number);
            Assert.NotEqual(1205, creation.SqlErrorNumber);

            // payment A succeeded
            Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentAId));
            Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderA));

            // No second pending payment was created for this discount.
            int pending = await _db.Seed.ScalarIntRawAsync(
                "SELECT COUNT(*) FROM Payments WHERE DiscountId = @D AND Status = 'Pending';",
                c => c.Parameters.Add("@D", System.Data.SqlDbType.SmallInt).Value = discountId);
            Assert.Equal(0, pending);

            // The successful payment must have incremented discount usage exactly once.
            int usage = (await _db.Seed.DiscountUsageAsync(discountId)) ?? 0;
            Assert.Equal(1, usage);
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
                int allowedUse = 1;
                int discountId = await _db.Seed.DiscountAsync(code, allowedUse, creator);

                int orderA = await _db.Seed.OrderAsync(userA, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderA, product, 30m);
                int paymentAId = await _db.Seed.PaymentAsync(orderA, $"idem-pa-{Guid.NewGuid():N}", "Pending", 30m, (short)discountId);

                int orderB = await _db.Seed.OrderAsync(userB, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderB, product, 30m);

                using var barrier = new Barrier(2);

                var completion = new OperationResult();
                var creation = new OperationResult();

                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        try
                        {
                            if (!barrier.SignalAndWait(BarrierTimeout))
                            {
                                completion.TimedOutOnBarrier = true;
                                completion.Exception = new TimeoutException(
                                    $"Concurrency barrier timed out after {BarrierTimeout.TotalSeconds} seconds during repeated iteration {i}.");
                                return;
                            }

                            var payment = await clsPayment.FindAsync(paymentAId);
                            completion.Success = await clsCheckoutService.CompletePaymentAsync(userA, payment, $"tx-{Guid.NewGuid():N}");
                        }
                        catch (SqlException ex)
                        {
                            completion.SqlErrorNumber = ex.Number;
                            completion.Exception = ex;
                        }
                        catch (Exception ex)
                        {
                            completion.Exception = ex;
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            if (!barrier.SignalAndWait(BarrierTimeout))
                            {
                                creation.TimedOutOnBarrier = true;
                                creation.Exception = new TimeoutException(
                                    $"Concurrency barrier timed out after {BarrierTimeout.TotalSeconds} seconds during repeated iteration {i}.");
                                return;
                            }

                            var p = new clsPayment();
                            p.SetPaymentMethod("card");
                            creation.Success = await p.CreatePayment(orderB, (short)discountId, $"idem-pb-{Guid.NewGuid():N}");
                        }
                        catch (SqlException ex)
                        {
                            creation.SqlErrorNumber = ex.Number;
                            creation.Exception = ex;
                        }
                        catch (Exception ex)
                        {
                            creation.Exception = ex;
                        }
                    }));

                Assert.False(completion.TimedOutOnBarrier, completion.Exception?.Message);
                Assert.False(creation.TimedOutOnBarrier, creation.Exception?.Message);
                Assert.Null(completion.Exception);
                Assert.True(completion.Success, $"iteration {i}: payment completion must succeed");
                Assert.NotEqual(1205, completion.SqlErrorNumber);

                Assert.False(creation.Success, $"iteration {i}: second reservation must not succeed");
                SqlException creationSqlException = Assert.IsType<SqlException>(creation.Exception);
                Assert.Equal(50004, creationSqlException.Number);

                Assert.Equal("Succeeded", await _db.Seed.PaymentStatusAsync(paymentAId));
                Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderA));

                int pending = await _db.Seed.ScalarIntRawAsync(
                    "SELECT COUNT(*) FROM Payments WHERE DiscountId = @D AND Status = 'Pending';",
                    c => c.Parameters.Add("@D", System.Data.SqlDbType.SmallInt).Value = discountId);
                Assert.Equal(0, pending);

                int usage = (await _db.Seed.DiscountUsageAsync(discountId)) ?? 0;
                Assert.Equal(1, usage);
                Assert.True(usage + pending <= allowedUse,
                    $"iteration {i}: usage({usage}) + pending({pending}) exceeded allowed({allowedUse})");
            }
        }

        [Fact]
        public async Task CompetingReservations_Repeated_NoDeadlock()
        {
            RequireDatabase();

            for (int i = 0; i < 15; i++)
            {
                int userA = await _db.Seed.UserAsync($"rep-a-{i}-{Guid.NewGuid():N}@test.local", "Student");
                int userB = await _db.Seed.UserAsync($"rep-b-{i}-{Guid.NewGuid():N}@test.local", "Student");
                int creator = await _db.Seed.UserAsync($"rep-c-{i}-{Guid.NewGuid():N}@test.local", "Instructor");
                int product = await _db.Seed.LessonProductAsync($"REP prod {i} {Guid.NewGuid():N}", 30m, creator, true);

                string code = $"RP{i}{Guid.NewGuid():N}"[..12];
                int allowedUse = 1;
                int discountId = await _db.Seed.DiscountAsync(code, allowedUse, creator);

                int orderA = await _db.Seed.OrderAsync(userA, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderA, product, 30m);
                int orderB = await _db.Seed.OrderAsync(userB, "Pending", 30m);
                await _db.Seed.OrderItemAsync(orderB, product, 30m);

                using var barrier = new Barrier(2);

                Func<int, Task<AttemptResult>> attempt = (orderId) => Task.Run(async () =>
                {
                    var result = new AttemptResult();
                    try
                    {
                        if (!barrier.SignalAndWait(BarrierTimeout))
                        {
                            result.TimedOutOnBarrier = true;
                            result.Exception = new TimeoutException(
                                $"Concurrency barrier timed out after {BarrierTimeout.TotalSeconds} seconds during repeated iteration {i}.");
                            return result;
                        }

                        var p = new clsPayment();
                        p.SetPaymentMethod("card");
                        result.Success = await p.CreatePayment(orderId, (short)discountId, $"idem-{Guid.NewGuid():N}");
                    }
                    catch (SqlException ex)
                    {
                        result.SqlErrorNumber = ex.Number;
                        result.Exception = ex;
                    }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                    }

                    return result;
                });

                AttemptResult[] results = await Task.WhenAll(attempt(orderA), attempt(orderB));

                Assert.False(results[0].TimedOutOnBarrier, results[0].Exception?.Message);
                Assert.False(results[1].TimedOutOnBarrier, results[1].Exception?.Message);

                int successes = results.Count(r => r.Success);
                int rejections = results.Count(r => r.SqlErrorNumber == 50004);
                int deadlocks = results.Count(r => r.SqlErrorNumber == 1205);
                AttemptResult? unexpected = results.FirstOrDefault(r =>
                    r.Exception is not null &&
                    !(r.Exception is SqlException sql && sql.Number == 50004));

                Assert.Equal(0, deadlocks);
                Assert.Null(unexpected?.Exception);
                Assert.Equal(1, successes);
                Assert.Equal(1, rejections);

                foreach (AttemptResult result in results)
                {
                    if (result.Success)
                    {
                        Assert.Null(result.Exception);
                    }
                    else
                    {
                        SqlException sqlException = Assert.IsType<SqlException>(result.Exception);
                        Assert.Equal(50004, sqlException.Number);
                    }
                }

                int pendingCount = await _db.Seed.ScalarIntRawAsync(
                    "SELECT COUNT(*) FROM Payments WHERE DiscountId = @D AND Status = 'Pending';",
                    c => c.Parameters.Add("@D", System.Data.SqlDbType.SmallInt).Value = discountId);
                Assert.Equal(1, pendingCount);

                int usage = (await _db.Seed.DiscountUsageAsync(discountId)) ?? 0;
                Assert.Equal(0, usage);
                Assert.True(usage + pendingCount <= allowedUse,
                    $"iteration {i}: usage({usage}) + pending({pendingCount}) exceeded allowed({allowedUse})");
            }
        }
    }
}