using Common.Dtos;
using Common.Exceptions;
using EduCore_BusinessLayer;
using Microsoft.AspNetCore.Mvc;
using EduCoreAPI.Helpers.Models.RequestModels;
using EduCoreAPI.Helpers.Mappers;
namespace EduCoreAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        // =========================
        // GET: Order by Id
        // =========================
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetOrderById([FromRoute] int id)
        {
            clsOrder order = await clsOrder.Find(id);
            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // GET: Pending Order For User
        // يجلب الكارت الحالي للمستخدم
        // =========================
        [HttpGet("cart/{userId:int}")]
        public async Task<ActionResult> GetCart([FromRoute] int userId)
        {
            clsOrder order = await clsOrder.FindOrderbyUserId(userId);
            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // POST: Add Item To Order
        // يضيف منتج للكارت
        // =========================
        
        [HttpPost("{Orderid:int}/items/{ItemId:int}")]
        public async Task<ActionResult> AddItemToOrder(
            [FromRoute] int Orderid,
            [FromRoute] int ItemId)
        {
            clsOrder order = await clsOrder.Find(Orderid);


            bool result = await order.AddItemToOrder(ItemId);

            if (!result)
                throw new ConflictException("Failed to add item to order");

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // DELETE: Remove Item From Order
        // يحذف منتج من الكارت
        // =========================
        [HttpDelete("{id:int}/items/{productId:int}")]
        public async Task<ActionResult> RemoveItemFromOrder(
            [FromRoute] int id,
            [FromRoute] int productId)
        {
            clsOrder order = await clsOrder.Find(id);

            bool result = await order.DeleteItemFromOrder(productId);

            if (!result)
                throw new ConflictException("Failed to remove item from order");

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // PUT: Complete Order
        // إتمام عملية الشراء
        // =========================
        [HttpPut("{id:int}/complete")]
        public async Task<ActionResult> CompleteOrder([FromRoute] int id)
        {
            clsOrder order = await clsOrder.Find(id);

            bool result = await order.MarkAsCompleted();

            if (!result)
                throw new ConflictException("Failed to complete order");

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // PUT: Cancel Order
        // إلغاء الطلب
        // =========================
        [HttpPut("{id:int}/cancel")]
        public async Task<ActionResult> CancelOrder([FromRoute] int id)
        {
            clsOrder order = await clsOrder.Find(id);

            bool result = await order.Cancel();

            if (!result)
                throw new ConflictException("Failed to cancel order");

            return Ok(new
            {
                order.Id,
                order.Status
            });
        }
    }
}