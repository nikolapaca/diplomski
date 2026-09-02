using FakeTrello.Model;
using FakeTrello.Model.Enum;

namespace FakeTrello.DTO
{
    public class BoardActivityDTO
    {
        public int Id { get; set; }

        public string Message { get; set; }

        public ActivityType Type { get; set; }

        public string CreatingUsername { get; set; }

        public int? CardId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}