using System.ComponentModel.DataAnnotations;

namespace Pivot.DTO
{
    public class PasswordChangeDTO
    {
        [Required]
        public string CurrentPassword {  get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password has to be at least 6 characters long.")]
        public string NewPassword { get; set; }

        [Required]
        public string ConfirmNewPassword { get; set; }
    }
}
