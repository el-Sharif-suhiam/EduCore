using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    public class BundleItemsViewModel 
    {
        public short Id {  get; set; }
        public string BundleName { get; set; }
            public List<CourseItemVM> Courses { get; set; }
        
    }

    public class CourseItemVM
    {
        public int CourseId { get; set; }
        public string Name { get; set; }
        public string? Summary { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
