using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    public class CourseWithInstructorViewModel
    {

        public int Id { get; set; }               // C.Id
        public string Title { get; set; }         // P.Name As Title
        public string? Summary { get; set; }      // P.Summary (nullable)
        public decimal BasePrice { get; set; }    // P.BasePrice (SMALLMONEY)
        public DateTime CreatedAt { get; set; }   // P.CreatedAt
        public string? ThumbnailUrl { get; set; } // P.ThumbnailUrl (nullable)
        public string? CoverImageUrl { get; set; }// C.CoverImageUrl (nullable)
        public List<InstructorsViewModel> CourseInstructors { get; set; }
        
    }

    public class InstructorsViewModel
    {
        public int InstructorId { get; set; }     // U.Id As InstructorId
        public string InstructorName { get; set; } // U.Name As InstructorName
    }
}
