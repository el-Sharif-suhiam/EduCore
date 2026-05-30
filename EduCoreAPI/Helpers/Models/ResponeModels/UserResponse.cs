using System;
using System.Collections.Generic;
using System.Text;

namespace EduCoreAPI.Helpers.Dtos.ResponeDto
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
                  
    }
}
