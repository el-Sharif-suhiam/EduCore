using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Utils;
using EduCore_DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
            DtoOrder dtoOrder = await clsOrderData.GetPendingOrderForUser(userId);

            if (dtoOrder != null)
            {
                List<DtoOrderItem> orderItems = await clsOrderItemData.GetItemsByOrderId(dtoOrder.Id);
                return new clsOrder(dtoOrder, orderItems);
            }
            else
                return new clsOrder();

        }

        public async Task<bool> AddItemToOrder(int productId)
        {
            if (Id > 0) {
                clsProduct product = await clsProduct.Find(productId);
                DtoOrderItemRespone newOrderItem =  await clsOrderItemData.AddItemToOrderAsync(Id, product.Id);
                
                if(newOrderItem == null)
                    throw new ConflictException("couldn't add item to the order");
           
                _items.Add(new DtoOrderItem
                {
                    Id = newOrderItem.Id,
                    OrderId = _order.Id,
                    ProductId = product.Id,
                    PriceAtPurchase = product.BasePrice
                });

                _order.TotalPrice = newOrderItem.NewOrderTotal;
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteItemFromOrder(int productId)
        {
            if (Id > 0)
            {
                clsProduct product = await clsProduct.Find(productId);
                decimal newTotal = await clsOrderItemData.RemoveItemFromAsync(Id, product.Id);
                DtoOrderItem? toDelItem = _items.Find(i=> i.ProductId == productId);
                _items.Remove(toDelItem);
                
                _order.TotalPrice = newTotal;
                return true;
            }
            return false;
        }



        public async Task<bool> MarkAsCompleted()
        {

            if (_order.Status == enOrderStatus.Cancelled || _order.Status == enOrderStatus.Empty)
                throw new ConflictException("Cannot complete a cancelled/empty order");

            bool result = await clsOrderData.UpdateStatus(_order.Id, enOrderStatus.Completed.ToString());
            if (result) _order.Status = enOrderStatus.Completed; 
            return result;
        }

        public async Task<bool> Cancel()
        {
            if (_order.Status == enOrderStatus.Completed)
                throw new ConflictException("Cannot cancel a completed order");

            bool result = await clsOrderData.UpdateStatus(_order.Id, enOrderStatus.Cancelled.ToString());
            if (result) _order.Status = enOrderStatus.Cancelled; 
            return result;
        }

    }
}