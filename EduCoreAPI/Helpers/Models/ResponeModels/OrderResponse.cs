using Common.Dtos;
using Common.Enums;

namespace EduCoreAPI.Helpers.Models.ResponeModels
{
    public class OrderResponse
    {
        public int Id {  get; set; }
        public int UserId {  get; set; }
        public decimal TotalPrice {  get; set; }
        public string Status {  get; set; }
        public DateTime CreatedAt {  get; set; }
        public List<DtoOrderItem> Items {  get; set; }
    }
}
