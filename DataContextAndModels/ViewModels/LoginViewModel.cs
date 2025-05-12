using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
