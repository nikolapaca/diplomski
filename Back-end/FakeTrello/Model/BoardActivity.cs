using FakeTrello.Model.Enum;

namespace FakeTrello.Model
{

    public class BoardActivity
    {
        public int Id { get; set; }

        public int BoardId { get; set; }
        public Board Board { get; set; }

        public int CreatingUserId { get; set; }
        public User CreatingUser { get; set; }

        public int? CardId { get; set; }
        public Card? Card { get; set; }

        public ActivityType Type { get; set; }

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public BoardActivity() { }
    }
}