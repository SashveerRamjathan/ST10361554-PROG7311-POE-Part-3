using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextAndModels.Models
{
    public class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }
        
        // navigation property
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
