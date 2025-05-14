using System.ComponentModel.DataAnnotations;

namespace DataContextAndModels.ViewModels
{
    public class LoginViewModel
    {
        [EmailAddress(ErrorMessage = "Not a valid email")]
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
