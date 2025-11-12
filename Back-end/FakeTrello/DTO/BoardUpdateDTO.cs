using System.ComponentModel.DataAnnotations;

namespace FakeTrello.DTO
{
    public class BoardUpdateDTO
    {
        [Required]
        [MinLength(1, ErrorMessage = "Name must contain at least one letter!")]
        public string OldBoardName { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Name must contain at least one letter!")]
        public string NewBoardName { get; set; }
        public string OwnerUsername { get; set; }
        public string OldBoardDescription { get; set; }
        public string NewBoardDescription { get; set; }
    }
}
