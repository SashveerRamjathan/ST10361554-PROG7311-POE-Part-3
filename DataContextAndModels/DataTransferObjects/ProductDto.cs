using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextAndModels.DataTransferObjects
{
    public class ProductDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public DateTime ProductionDate { get; set; }

        // Foreign keys
        public string FarmerId { get; set; }

        public string CategoryId { get; set; }

        // Extra Info properties
        public string FarmerName { get; set; }

        public string CategoryName { get; set; }
    }
}
