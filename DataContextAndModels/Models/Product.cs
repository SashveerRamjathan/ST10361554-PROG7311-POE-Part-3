using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextAndModels.Models
{
    public class Product
    {
        public string Id { get; set; }

        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public DateTime ProductionDate { get; set; }

        // Foreign keys
        public string FarmerId { get; set; } = null!;

        public string CategoryId { get; set; } = null!;

        // navigation property
        public virtual ApplicationUser Farmer { get; set; } = null!;

        public virtual Category Category { get; set; } = null!;
    }
}
