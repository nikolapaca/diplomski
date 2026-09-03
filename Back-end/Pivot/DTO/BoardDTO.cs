using Pivot.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace Pivot.DTO
{
    public class BoardDTO
    {
        [Required]
        [MinLength(1, ErrorMessage = "Name must contain at least one letter!")]
        public string Name { get; set; }

        public string Description { get; set; }

        public BoardStatus Status { get; set; }
        public string OwnerUsername { get; set; }
        public bool IsFavorite { get; set; }
    }
}
