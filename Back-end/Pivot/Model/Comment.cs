using Pivot.Model.Enum;

namespace Pivot.Model
{
    public class Comment
    {
        public int Id { get; set; }

        public int CardId { get; set; }
        public Card Card { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public EntityStatus Status { get; set; } = EntityStatus.ACTIVE;

        public Comment() { }
    }
}