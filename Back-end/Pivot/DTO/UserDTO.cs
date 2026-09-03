using System.ComponentModel.DataAnnotations;

namespace Pivot.DTO
{
    public class UserDTO
    {
        [Required]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters long.")]
        public string Name { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "Surname must be at least 2 characters long.")]
        public string Surname { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "Username must be at least 2 characters long.")]
        public string Username {  get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password has to be at least 6 characters long.")]
        public string Password { get; set; }
    }
}
