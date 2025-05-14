using System.ComponentModel.DataAnnotations;

namespace DataContextAndModels.ViewModels
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Category name is required")]
        public string Name { get; set; }
    }
}
