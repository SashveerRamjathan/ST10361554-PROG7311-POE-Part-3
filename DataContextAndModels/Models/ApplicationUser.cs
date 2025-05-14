using Microsoft.AspNetCore.Identity;

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
