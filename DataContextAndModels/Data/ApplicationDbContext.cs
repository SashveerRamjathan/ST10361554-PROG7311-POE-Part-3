using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DataContextAndModels.Models;

namespace DataContextAndModels.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            // Configure the one-to-many relationship between ApplicationUser and Product
            builder.Entity<Product>()
                .HasOne(p => p.Farmer)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.FarmerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the many-to-one relationship between Product and Category
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}
