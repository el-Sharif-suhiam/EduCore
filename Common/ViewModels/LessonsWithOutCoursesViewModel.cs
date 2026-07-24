using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    public class LessonsWithOutCoursesViewModel
    {
        public int Id { get; set; } //    L.Id
        public string Name { get; set; }  //    P.Name,
        public string? Summary { get; set; } //   P.Summary,
	    public decimal BasePrice { get; set; } // P.BasePrice,
	    public DateTime CreatedAt { get; set; } // P.CreatedAt, 
	    public string? ThumbnailUrl { get; set; } // P.ThumbnailUrl, 
	    public bool IsPublished { get; set; } // P.IsPublished, 
	    public int InstructorId { get; set; } // U.Id AS InstructorId,
        public string InstructorName { get; set; } // U.Name AS InstructorName
    }

    
}
