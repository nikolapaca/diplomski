using System.ComponentModel.DataAnnotations;

namespace FakeTrello.DTO
{
    public class CommentDTO
    {
        public int Id { get; set; }

        public int CardId { get; set; }

        public string? Username { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Comment cannot be empty.")]
        [MaxLength(1000, ErrorMessage = "Comment cannot be longer than 1000 characters.")]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}