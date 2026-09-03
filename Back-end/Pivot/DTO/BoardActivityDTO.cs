using Pivot.Model;
using Pivot.Model.Enum;

namespace Pivot.DTO
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