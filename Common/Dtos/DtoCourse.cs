using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoCourse
    {
       public int Id { get; set; }
       public int ProductId {  get; set; }
	   public string? CoverImageUrl  {  get; set; }
       public bool IsDeleted {  get; set; }
	    public DateTime? DeletedAt {  get; set; }
	    public int? DeletedById { get; set; }
    }
    
}
