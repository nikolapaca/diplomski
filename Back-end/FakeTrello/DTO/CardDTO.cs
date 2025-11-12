using FakeTrello.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace FakeTrello.DTO
{
    public class CardDTO
    {
        public int Id { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Name must contain at least one letter!")]
        public string Name { get; set; }
        public string Description { get; set; }
        public int CardListId { get; set; }
        public EntityStatus Status { get; set; }
        public string? AssignedUserUsername { get; set; }
        public int Index { get; set; }
    }
}
