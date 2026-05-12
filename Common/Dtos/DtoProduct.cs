using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoProduct
    {
        public int Id { get; set; }
        public byte ProductType { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal BasePrice  { get; set; }
        public int CreatedByAdmin { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Summary { get; set; }
        public bool IsPublished  { get; set; }
    }
}
