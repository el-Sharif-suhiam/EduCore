using Common.Dtos;
using Common.Exceptions;
using EduCore_BusinessLayer;
using Microsoft.AspNetCore.Mvc;
using EduCoreAPI.Helpers.Models.RequestModels;
using EduCoreAPI.Helpers.Mappers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace EduCoreAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        // =========================
        // POST: Create Order (cart)
        // ينشئ سلة جديدة للمستخدم الحالي
        // =========================
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> CreateOrder()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int currentUserId))
                throw new UnauthorizedAccessException("Invalid user identity");

            clsOrder existing = await clsOrder.FindOrderbyUserId(currentUserId);

            if (existing.Id > 0)
                return Ok(OrderMapper.ToOrderResponse(existing));

            clsOrder order = new clsOrder();
            order.SetUserId(currentUserId);

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            bool result = await order.CreateAsync(ip, userAgent);

            if (!result)
                throw new ConflictException("Failed to create order");

            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = order.Id },
                OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // GET: Order by Id
        // =========================
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetOrderById([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsOrder order = await clsOrder.Find(id);
            var authResult = await authorizationService.AuthorizeAsync(
               User,
               order.UserId,
               "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // GET: Pending Order For User
        // يجلب الكارت الحالي للمستخدم
        // =========================
        [Authorize]
        [HttpGet("cart/{userId:int}")]
        public async Task<ActionResult> GetCart([FromRoute] int userId, [FromServices] IAuthorizationService authorizationService)
        {
            clsOrder order = await clsOrder.FindOrderbyUserId(userId);
            var authResult = await authorizationService.AuthorizeAsync(
              User,
              order.UserId,
              "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // POST: Add Item To Order
        // يضيف منتج للكارت
        // =========================
        [Authorize]
        [HttpPost("{Orderid:int}/items/{ItemId:int}")]
        public async Task<ActionResult> AddItemToOrder(
            [FromRoute] int Orderid,
            [FromRoute] int ItemId, [FromServices] IAuthorizationService authorizationService)
        {
            clsOrder order = await clsOrder.Find(Orderid);

            var authResult = await authorizationService.AuthorizeAsync(
             User,
             order.UserId,
             "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403
            bool result = await order.AddItemToOrder(ItemId);

            if (!result)
                throw new ConflictException("Failed to add item to order");

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // DELETE: Remove Item From Order
        // يحذف منتج من الكارت
        // =========================
        [Authorize]
        [HttpDelete("{id:int}/items/{productId:int}")]
        public async Task<ActionResult> RemoveItemFromOrder(
            [FromRoute] int id,
            [FromRoute] int productId, [FromServices] IAuthorizationService authorizationService)
        {
            clsOrder order = await clsOrder.Find(id);
            var authResult = await authorizationService.AuthorizeAsync(
             User,
             order.UserId,
             "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403
            bool result = await order.DeleteItemFromOrder(productId);

            if (!result)
                throw new ConflictException("Failed to remove item from order");

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // PUT: Complete Order
        // إتمام عملية الشراء
        // =========================
        [Authorize]
        [HttpPut("{id:int}/complete")]
        public async Task<ActionResult> CompleteOrder([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsOrder order = await clsOrder.Find(id);
            var authResult = await authorizationService.AuthorizeAsync(
           User,
           order.UserId,
           "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403

            bool result = await order.MarkAsCompleted();

            if (!result)
                throw new ConflictException("Failed to complete order");

            return Ok(OrderMapper.ToOrderResponse(order));
        }

        // =========================
        // PUT: Cancel Order
        // إلغاء الطلب
        // =========================
        [Authorize]
        [HttpPut("{id:int}/cancel")]
        public async Task<ActionResult> CancelOrder([FromRoute] int id, [FromServices] IAuthorizationService authorizationService)
        {
            clsOrder order = await clsOrder.Find(id);
            var authResult = await authorizationService.AuthorizeAsync(
           User,
           order.UserId,
           "UserOwnerOrAdmin");

            if (!authResult.Succeeded)
                return Forbid(); // 403
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