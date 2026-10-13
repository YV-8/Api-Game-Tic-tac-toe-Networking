using System.ComponentModel.DataAnnotations;

namespace GameAPI.Models
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(72, ErrorMessage = "Password cannot exceed 72 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
