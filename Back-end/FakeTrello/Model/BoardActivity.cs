namespace FakeTrello.Model
{
    public enum ActivityType
    {
        CARD_CREATED,
        CARD_UPDATED,
        CARD_DELETED,
        CARD_MOVED,
        CARD_REORDERED,
        USER_ASSIGNED_TO_CARD,
        USER_UNASSIGNED_FROM_CARD,
        COLLABORATOR_ADDED,
        COLLABORATOR_REMOVED,
        LIST_CREATED,
        LIST_UPDATED,
        LIST_DELETED,
        BOARD_UPDATED,
        BOARD_ARCHIVED,
        LIST_PINNED,
        LIST_UNPINNED,
        CARD_PINNED,
        CARD_UNPINNED,
        ATTACHMENT_ADDED,
        ATTACHMENT_REMOVED
    }

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