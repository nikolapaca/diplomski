using FakeTrello.Model;

namespace FakeTrello.DTO
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string Message { get; set; }

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? BoardName { get; set; }

        public string? BoardOwnerUsername { get; set; }

        public int? CardId { get; set; }
    }
}
