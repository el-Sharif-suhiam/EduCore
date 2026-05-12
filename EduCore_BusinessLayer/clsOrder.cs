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
        enMode _Mode;
        private DtoOrder _order;
        private List<DtoOrderItem> _items;
        List<DtoOrderItem> _toBeAdded;
        public int Id => _order.Id;
        public int UserId => _order.UserId;
        public decimal TotalPrice => _order.TotalPrice;
        public enOrderStatus Status => _order.Status;
        public DateTime CreatedAt => _order.CreatedAt;

        public clsOrder()
        {
            _order = new DtoOrder();
            _items = new List<DtoOrderItem>();
            _toBeAdded = new List<DtoOrderItem>();
            _order.Status = enOrderStatus.Pending;
            _Mode = enMode.Add;
        }

        private clsOrder(DtoOrder dto, List<DtoOrderItem> items)
        {
            _order = dto;
            _items = items ?? new List<DtoOrderItem>();
            _toBeAdded = new List<DtoOrderItem>();
            _Mode = enMode.Update;
        }

        public void SetUserId(int userId)
        {
            if (userId <= 0)
                throw new ValidationException("User id is not valid");
            _order.UserId = userId;
        }

        public static clsOrder Find(int orderId)
        {
            var order = clsOrderData.GetOrderById(orderId);
            if (order == null)
                throw new NotFoundException("No order found with this id");

            var items = clsOrderItemData.GetItemsByOrderId(orderId);

            return new clsOrder(order, items);
        }



        public void AddItem(int productId, decimal price)
        {
            if (price <= 0)
                throw new Exception("Invalid price");
            if (_items.Any(i => i.ProductId == productId && !i.MarkedToDelete))
                throw new ConflictException("Product already exists in order");

            if (_toBeAdded.Any(i => i.ProductId == productId))
                throw new ConflictException("Product already queued");

            _toBeAdded.Add(new DtoOrderItem
            {
                ProductId = productId,
                PriceAtPurchase = price,
            });
        }

        public bool DeleteItem(int productId)
        {
            bool found = false;
            int removed = _toBeAdded.RemoveAll(i => i.ProductId == productId);
            if(removed > 0)
                found = true;

            foreach (var item in _items)
            {
                if (item.ProductId == productId && !item.MarkedToDelete)
                {
                    item.MarkedToDelete = true;
                    found = true;
                }
            }

            if (!found)
                throw new NotFoundException("Item not found");
            return found;
            
        }


        
        private decimal _CalculateTotal(List<DtoOrderItem> items, List<DtoOrderItem> toAdd)
        {
            return items.Where(i => !i.MarkedToDelete).Sum(i => i.PriceAtPurchase ?? 0)
                 + toAdd.Sum(i => i.PriceAtPurchase ?? 0);
        }
        

        bool _AddOrder()
        {
            return clsGeneralData.ExecuteTransaction((conn, tx) =>
            {
                
                _order.TotalPrice = _CalculateTotal(_items,_toBeAdded);
                if (_order.TotalPrice <= 0)
                    throw new ValidationException("Order cannot be empty");

                int orderId = clsOrderData.CreateOrder(_order, conn, tx);

                if (orderId <= 0)
                    throw new Exception("Failed to create order");

                _order.Id = orderId;
                var insertedItems = new List<DtoOrderItem>();

               
                    foreach(var Additem in _toBeAdded)
                    {
                       Additem.OrderId = orderId;
                        int itemId = clsOrderItemData.AddItemToOrder(Additem, conn, tx);
                        if (itemId <= 0)
                            throw new Exception("Failed to insert order item");
                        Additem.Id = itemId;
                        insertedItems.Add(Additem);
                    }
                
                _items.AddRange(insertedItems);
                _toBeAdded.Clear();
                

                _Mode = enMode.Update;
                return true;
            });
        }

        bool _UpdateOrder()
        {
            return clsGeneralData.ExecuteTransaction((conn, tx) =>
            {
                var remainingItems = new List<DtoOrderItem>();

                foreach (var item in _items)
                {

                    if (item.MarkedToDelete)
                    {
                        if (!clsOrderItemData.DeleteItemFromOrder(item.Id, conn, tx))
                            throw new ConflictException($"couldn't delete item with id:{item.Id}");

                    }
                    else
                    {
                        remainingItems.Add(item);
                    }
                }

                var insertedItems = new List<DtoOrderItem>();

                for (int i = _toBeAdded.Count - 1; i >= 0; i--)
                {
                    DtoOrderItem Additem = _toBeAdded[i];
                    Additem.OrderId = Id;
                    int itemId = clsOrderItemData.AddItemToOrder(Additem, conn, tx);
                    if (itemId <= 0)
                        throw new Exception("Failed to insert order item");
                    Additem.Id = itemId;
                    insertedItems.Add(Additem);
                }
                decimal total = _CalculateTotal(remainingItems, insertedItems);

                if (total <= 0)
                    throw new ValidationException("Order cannot be empty");

                _order.TotalPrice = total;
                
                if (!clsOrderData.UpdateTotal(Id, _order.TotalPrice,conn,tx))
                    throw new ConflictException("Faild to update order total price");

                // after the state is sucess
                _items = remainingItems;
                _items.AddRange(insertedItems);
                _toBeAdded.Clear();
                return true;
            });
        }
        public bool Save()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    return _AddOrder();
                case enMode.Update:
                    return _UpdateOrder();
                default: return false;
            }
        }


        public bool MarkAsCompleted()
        {
            if (_toBeAdded.Any())
                throw new ConflictException("Save order first");

            if (_order.Status == enOrderStatus.Cancelled)
                throw new ConflictException("Cannot complete a cancelled order");

            bool result = clsOrderData.UpdateStatus(_order.Id, enOrderStatus.Completed.ToString());
            if (result) _order.Status = enOrderStatus.Completed; 
            return result;
        }

        public bool Cancel()
        {
            if (_order.Status == enOrderStatus.Completed)
                throw new ConflictException("Cannot cancel a completed order");

            bool result = clsOrderData.UpdateStatus(_order.Id, enOrderStatus.Cancelled.ToString());
            if (result) _order.Status = enOrderStatus.Cancelled; 
            return result;
        }

    }
}