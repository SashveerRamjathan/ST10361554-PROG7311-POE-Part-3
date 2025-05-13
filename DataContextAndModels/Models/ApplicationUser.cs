using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextAndModels.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Address { get; set; }

        public string? FullName { get; set; }

        // navigation property to represent the one-to-many relationship with Product
        public virtual ICollection<Product>? Products { get; set; } = new List<Product>();
    }
}
