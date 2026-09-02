using FakeTrello.Model.Enum;

namespace FakeTrello.Model
{
    public class CardImage
    {
        public int Id { get; set; }

        public int CardId { get; set; }
        public Card Card { get; set; }

        public string FileName { get; set; }

        public string FilePath { get; set; }

        public int UploadedByUserId { get; set; }
        public User UploadedByUser { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public EntityStatus Status { get; set; } = EntityStatus.ACTIVE;

        public CardImage() { }
    }
}