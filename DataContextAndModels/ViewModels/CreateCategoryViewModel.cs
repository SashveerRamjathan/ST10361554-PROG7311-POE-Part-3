using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextAndModels.ViewModels
{
    public class CreateCategoryViewModel
    {

        public CreateCategoryViewModel(string categoryName)
        {
            Id = Guid.NewGuid().ToString();
            Name = categoryName;
        }

        [Required(ErrorMessage = "Category Id is required")]
        public string Id { get; set; }


        [Required(ErrorMessage = "Category name is required")]
        public string Name { get; set; }
    }
}
