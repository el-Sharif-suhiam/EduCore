using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Models.ResponeModels;

namespace EduCoreAPI.Helpers.Mappers
{
    public class OrderMapper
    {
        public static OrderResponse ToOrderResponse(clsOrder order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                CreatedAt = order.CreatedAt,
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                Items = order.Items,
                UserId = order.UserId,
            };
        }
    }
}
