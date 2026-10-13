using System.ComponentModel.DataAnnotations;

namespace GameAPI.Models
{
    public class UserDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(72, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 72 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
