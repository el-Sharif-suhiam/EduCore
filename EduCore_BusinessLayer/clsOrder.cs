
using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;

namespace EduCore_BusinessLayer
{
    public class clsOrder
    {
        private DtoOrder _order;
        private List<DtoOrderItem> _items;

        public int Id => _order.Id;
        public int UserId => _order.UserId;
        public decimal TotalPrice => _order.TotalPrice;
        public enOrderStatus Status => _order.Status;
        public DateTime CreatedAt => _order.CreatedAt;
        public List<DtoOrderItem> Items => _items;

        public clsOrder()
        {
            _order = new DtoOrder();
            _items = new List<DtoOrderItem>();

            _order.Status = enOrderStatus.Pending;
        }

        private clsOrder(DtoOrder dto, List<DtoOrderItem> items)
        {
            _order = dto;
            _items = items ?? new List<DtoOrderItem>();
        }

        public void SetUserId(int userId)
        {
            if (userId <= 0)
                throw new ValidationException("User id is not valid");

            _order.UserId = userId;
        }

        // =========================
        // CREATE ORDER
        // =========================

        public async Task<bool> CreateAsync(
    string? ipAddress = null,
    string? userAgent = null)
        {
            if (_order.UserId <= 0)
                throw new ValidationException("User id is required");

            if (_order.Id > 0)
                throw new ConflictException("Order already exists");

            _order.Status = enOrderStatus.Pending;
            _order.TotalPrice = 0;

            int orderId = await clsOrderData.CreateOrder(_order);

            if (orderId <= 0)
                throw new ConflictException("Failed to create order");

            _order.Id = orderId;

            await clsAudit.LogAsync(
                _order.UserId,
                enAuditActionType.CreateOrder,
                "Order",
                orderId,
                "Created empty order",
                ipAddress,
                userAgent);

            return orderId > 0;
        }

        // =========================
        // FIND
        // =========================

        public static async Task<clsOrder> Find(int orderId)
        {
            var order = await clsOrderData.GetOrderById(orderId);

            if (order == null)
                throw new NotFoundException("No order found with this id");

            var items = await clsOrderItemData.GetItemsByOrderId(orderId);

            return new clsOrder(order, items);
        }

        public static async Task<clsOrder> FindOrderbyUserId(int userId)
        {
            DtoOrder dtoOrder =
                await clsOrderData.GetPendingOrderForUser(userId);

            if (dtoOrder != null)
            {
                List<DtoOrderItem> orderItems =
                    await clsOrderItemData.GetItemsByOrderId(dtoOrder.Id);

                return new clsOrder(dtoOrder, orderItems);
            }

            return new clsOrder();
        }

        // =========================
        // ADD ITEM
        // =========================

        public async Task<bool> AddItemToOrder(
            int productId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (Id <= 0)
                throw new ValidationException("Order is not created");

            clsProduct product = await clsProduct.Find(productId);

            if (!product.IsPublished)
                throw new ConflictException("Product is not available for purchase");

            DtoOrderItemRespone newOrderItem =
                await clsOrderItemData.AddItemToOrderAsync(Id, product.Id);

            if (newOrderItem == null)
                throw new ConflictException("Couldn't add item to the order");

            _items.Add(new DtoOrderItem
            {
                Id = newOrderItem.Id,
                OrderId = _order.Id,
                ProductId = product.Id,
                PriceAtPurchase = product.BasePrice
            });

            _order.TotalPrice = newOrderItem.NewOrderTotal;

            await clsAudit.LogAsync(
                _order.UserId,
                enAuditActionType.UpdateOrder,
                "Order",
                _order.Id,
                $"Added product id {productId} to order",
                ipAddress,
                userAgent);

            return true;
        }

        // =========================
        // DELETE ITEM
        // =========================

        public async Task<bool> DeleteItemFromOrder(
            int productId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (Id <= 0)
                throw new ValidationException("Order is not created");

            DtoOrderItem? toDelete =
                _items.Find(i => i.ProductId == productId);

            if (toDelete == null)
                throw new NotFoundException("This product is not in the order");

            clsProduct product = await clsProduct.Find(productId);

            decimal newTotal =
                await clsOrderItemData.RemoveItemFromAsync(Id, product.Id);

            _items.Remove(toDelete);

            _order.TotalPrice = newTotal;

            await clsAudit.LogAsync(
                _order.UserId,
                enAuditActionType.UpdateOrder,
                "Order",
                _order.Id,
                $"Removed product id {productId} from order",
                ipAddress,
                userAgent);

            return true;
        }

        // =========================
        // COMPLETE
        // =========================

        public async Task<bool> MarkAsCompleted(
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (_order.Status == enOrderStatus.Cancelled ||
                _order.Status == enOrderStatus.Empty)
            {
                throw new ConflictException(
                    "Cannot complete a cancelled/empty order");
            }

            if (_order.Status == enOrderStatus.Completed)
                return true;

            if (!await clsPaymentData.HasSucceededPaymentForOrder(_order.Id))
            {
                throw new ConflictException(
                    "Order cannot be completed without a succeeded payment");
            }

            bool result = await clsOrderData.UpdateStatus(
                _order.Id,
                enOrderStatus.Completed.ToString());

            if (!result)
                return false;

            _order.Status = enOrderStatus.Completed;

            await clsAudit.LogAsync(
                _order.UserId,
                enAuditActionType.UpdateOrder,
                "Order",
                _order.Id,
                "Order marked as completed",
                ipAddress,
                userAgent);

            return true;
        }

        // =========================
        // CANCEL
        // =========================

        public async Task<bool> Cancel(
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (_order.Status == enOrderStatus.Completed)
            {
                throw new ConflictException(
                    "Cannot cancel a completed order");
            }

            if (_order.Status == enOrderStatus.Cancelled)
                return true;

            bool result = await clsGeneralData.ExecuteTransaction(
                async (conn, tx) =>
                {
                    bool updated = await clsOrderData.UpdateStatus(
                        _order.Id,
                        enOrderStatus.Cancelled.ToString(),
                        conn,
                        tx);

                    if (!updated)
                        return false;

                    await clsPaymentData.ExpirePendingPaymentsForOrder(
                        _order.Id,
                        conn,
                        tx);

                    return true;
                });

            if (!result)
                return false;

            _order.Status = enOrderStatus.Cancelled;

            await clsAudit.LogAsync(
                _order.UserId,
                enAuditActionType.UpdateOrder,
                "Order",
                _order.Id,
                "Order cancelled",
                ipAddress,
                userAgent);

            return true;
        }
    }
}