using System;
using System.Threading.Tasks;
using Common.Exceptions;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using Microsoft.Data.SqlClient;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Regression tests for Issue B: order modifications used to be allowed
    /// in any state, and the stored procedures silently reset Completed/
    /// Cancelled orders back to Pending. The state machine must hold in
    /// BOTH the business layer and SQL Server.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class OrderStateTests
    {
        private readonly DatabaseFixture _db;

        public OrderStateTests(DatabaseFixture db) => _db = db;

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private async Task<int> PublishedProductAsync(int createdBy, decimal price = 25m)
            => await _db.Seed.LessonProductAsync($"Product {Guid.NewGuid():N}", price, createdBy, true);

        [Fact]
        public async Task AddItemToOrder_PendingOrder_Succeeds()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-p-{Guid.NewGuid():N}@test.local", "Student");
            int product = await PublishedProductAsync(user);

            int orderId = await _db.Seed.OrderAsync(user, "Pending", 0m);

            var order = await clsOrder.Find(orderId);
            bool added = await order.AddItemToOrder(product);

            Assert.True(added);
            Assert.Single(order.Items);
            Assert.Equal("Pending", await _db.Seed.OrderStatusAsync(orderId));
        }

        [Fact]
        public async Task AddItemToOrder_EmptyOrder_Succeeds()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-e-{Guid.NewGuid():N}@test.local", "Student");
            int product = await PublishedProductAsync(user);

            int orderId = await _db.Seed.OrderAsync(user, "Empty", 0m);

            var order = await clsOrder.Find(orderId);
            bool added = await order.AddItemToOrder(product);

            Assert.True(added);
            Assert.Equal("Pending", await _db.Seed.OrderStatusAsync(orderId));
        }

        [Fact]
        public async Task AddItemToOrder_CompletedOrder_ThrowsConflict()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-c-{Guid.NewGuid():N}@test.local", "Student");
            int product = await PublishedProductAsync(user);

            int orderId = await _db.Seed.OrderAsync(user, "Completed", 100m);

            var order = await clsOrder.Find(orderId);

            await Assert.ThrowsAsync<ConflictException>(() => order.AddItemToOrder(product));
            Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderId));
        }

        [Fact]
        public async Task AddItemToOrder_CancelledOrder_ThrowsConflict()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-x-{Guid.NewGuid():N}@test.local", "Student");
            int product = await PublishedProductAsync(user);

            int orderId = await _db.Seed.OrderAsync(user, "Cancelled", 50m);

            var order = await clsOrder.Find(orderId);

            await Assert.ThrowsAsync<ConflictException>(() => order.AddItemToOrder(product));
            Assert.Equal("Cancelled", await _db.Seed.OrderStatusAsync(orderId));
        }

        [Fact]
        public async Task DeleteItemFromOrder_CompletedOrder_ThrowsConflict()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-d-{Guid.NewGuid():N}@test.local", "Student");
            int product = await PublishedProductAsync(user);

            int orderId = await _db.Seed.OrderAsync(user, "Completed", 25m);
            await _db.Seed.OrderItemAsync(orderId, product, 25m);

            var order = await clsOrder.Find(orderId);

            await Assert.ThrowsAsync<ConflictException>(() => order.DeleteItemFromOrder(product));
            Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderId));
        }

        [Fact]
        public async Task RemoveLastItem_PendingOrder_TransitionsToEmpty()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-r-{Guid.NewGuid():N}@test.local", "Student");
            int product = await PublishedProductAsync(user);

            int orderId = await _db.Seed.OrderAsync(user, "Pending", 25m);
            await _db.Seed.OrderItemAsync(orderId, product, 25m);

            var order = await clsOrder.Find(orderId);
            bool removed = await order.DeleteItemFromOrder(product);

            Assert.True(removed);
            Assert.Equal("Empty", await _db.Seed.OrderStatusAsync(orderId));
        }

        [Fact]
        public async Task StoredProcedures_RejectCompletedOrder_AddAndRemove()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-sp-{Guid.NewGuid():N}@test.local", "Student");
            int product = await PublishedProductAsync(user);

            int orderId = await _db.Seed.OrderAsync(user, "Completed", 100m);

            // The SQL guard must hold even when called directly (defence in
            // depth): Completed orders can never accept items again.
            await using SqlConnection con = new(_db.TestDatabaseConnectionString);
            await con.OpenAsync();

            await using (SqlCommand add = new("SP_AddNewItemToOrder", con) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                add.Parameters.AddWithValue("@OrderId", orderId);
                add.Parameters.AddWithValue("@ProductId", product);

                SqlException rejected = await Assert.ThrowsAsync<SqlException>(() => add.ExecuteNonQueryAsync());
                Assert.Contains("not editable", rejected.Message, StringComparison.OrdinalIgnoreCase);
            }

            await using (SqlCommand remove = new("SP_RemoveItemFromOrder", con) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                remove.Parameters.AddWithValue("@OrderId", orderId);
                remove.Parameters.AddWithValue("@ProductId", product);

                SqlException rejected = await Assert.ThrowsAsync<SqlException>(() => remove.ExecuteNonQueryAsync());
                Assert.Contains("not editable", rejected.Message, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Equal("Completed", await _db.Seed.OrderStatusAsync(orderId));
        }

        [Fact]
        public async Task AddItemToOrder_UnpublishedProduct_ThrowsAndLeavesOrderUntouched()
        {
            RequireDatabase();

            int user = await _db.Seed.UserAsync($"ord-u-{Guid.NewGuid():N}@test.local", "Student");
            int draftProduct = await _db.Seed.LessonProductAsync($"Draft {Guid.NewGuid():N}", 25m, user, false);

            int orderId = await _db.Seed.OrderAsync(user, "Pending", 0m);

            var order = await clsOrder.Find(orderId);

            await Assert.ThrowsAsync<ConflictException>(() => order.AddItemToOrder(draftProduct));

            Assert.Empty(order.Items);
            Assert.Equal("Pending", await _db.Seed.OrderStatusAsync(orderId));
        }
    }
}
