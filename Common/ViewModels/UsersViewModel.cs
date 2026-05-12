using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ViewModels
{
    public class UsersViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public string Role {  get; set; }
    }
}
