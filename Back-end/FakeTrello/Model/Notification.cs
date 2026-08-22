namespace FakeTrello.Model
{

    public enum NotificationType
    {
        ADDED_TO_BOARD,
        REMOVED_FROM_BOARD,
        ASSIGNED_TO_CARD,
        UNASSIGNED_FROM_CARD,
        BOARD_DELETED,
        BOARD_ARCHIVED,
        NEW_COMMENT_ON_CARD
    }

    public class Notification
    {
        public int Id { get; set; }

        public int RecipientUserId { get; set; }
        public User RecipientUser { get; set; }

        public int CreatingUserId { get; set; }
        public User CreatingUser { get; set; }

        public int? BoardId { get; set; }
        public Board? Board { get; set; }

        public int? CardId { get; set; }
        public Card? Card { get; set; }

        public NotificationType Type { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Notification() { }

    }
}
