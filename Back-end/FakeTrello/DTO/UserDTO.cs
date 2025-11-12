using System.ComponentModel.DataAnnotations;

namespace FakeTrello.DTO
{
    public class UserDTO
    {
        [Required]
        [MinLength(1, ErrorMessage = "Name cannot be empty.")]
        public string Name { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Surname cannot be empty.")]
        public string Surname { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Username {  get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password has to be at least 6 characters long.")]
        public string Password { get; set; }
    }
}
