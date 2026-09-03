using System.ComponentModel.DataAnnotations;

namespace Pivot.DTO
{
    public class ForgotPasswordRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
