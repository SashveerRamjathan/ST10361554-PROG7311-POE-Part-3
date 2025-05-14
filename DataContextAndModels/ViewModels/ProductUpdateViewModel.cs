using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextAndModels.ViewModels
{
    public class ProductUpdateViewModel
    {

        [Required(ErrorMessage = "Product Id is required")]
        public string Id { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(100, ErrorMessage = "Product Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Product Description is required")]
        [StringLength(500, ErrorMessage = "Product Description cannot be longer than 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Product Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Product Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Product Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Product Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Production Date is required")]
        [DataType(DataType.Date)]
        public DateTime ProductionDate { get; set; }

        [Required(ErrorMessage = "CategoryId is required")]
        public string CategoryId { get; set; }
    }
}
