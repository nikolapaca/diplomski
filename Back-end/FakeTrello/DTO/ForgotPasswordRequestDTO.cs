using System.ComponentModel.DataAnnotations;

namespace FakeTrello.DTO
{
    public class ForgotPasswordRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
