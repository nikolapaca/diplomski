using Pivot.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace Pivot.Model
{
    public class CardList
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BoardId { get; set; }
        public EntityStatus Status {  get; set; }
        public Board Board { get; set; }
        public int Index { get; set; }
        public bool IsPinned { get; set; } = false;
        public ICollection<Card> Cards { get; set; }

        [Timestamp]
        public uint Version { get; set; }
        public CardList() { }
        public CardList(string name, EntityStatus status)
        {
            Name = name;
            Status = status;
        }
    }
}
