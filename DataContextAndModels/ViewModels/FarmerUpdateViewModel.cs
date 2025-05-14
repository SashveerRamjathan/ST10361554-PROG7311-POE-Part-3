using System.ComponentModel.DataAnnotations;

namespace DataContextAndModels.ViewModels
{
    public class FarmerUpdateViewModel
    {
        public string Id { get; set; }

        [EmailAddress(ErrorMessage = "Email Address is not valid")]
        [Required(ErrorMessage = "Email is required")]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Not a valid Phone Number")]
        public string PhoneNumber { get; set; }
    }
}
