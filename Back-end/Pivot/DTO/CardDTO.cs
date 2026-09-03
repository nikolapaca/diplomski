using Pivot.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace Pivot.DTO
{
    public class CardDTO
    {
        public int Id { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Name must contain at least one letter!")]
        public string Name { get; set; }
        public string Description { get; set; }
        public int CardListId { get; set; }
        public int CreatedByUserId { get; set; }
        public string? CreatedByUsername { get; set; }
        public EntityStatus Status { get; set; }
        public List<string?> AssignedUserUsernames { get; set; } = new();
        public int Index { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? DueDate { get; set; }
        public string? CoverImageUrl { get; set; }
        public int AttachmentCount { get; set; }
    }
}