using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    public class BundleViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt {  get; set; }
        public decimal BasePrice {  get; set; }
        public string? ThumbnailUrl {  get; set; }
        public string? Summary {  get; set; }

    }
}
