using FakeTrello.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace FakeTrello.Model
{
    public class Card
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CardListId { get; set; }
        public CardList CardList { get; set; }
        public int Index { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }
        public ICollection<CardAssignee> Assignees { get; set; } = new List<CardAssignee>();
        public EntityStatus Status { get; set; }
        public bool IsPinned { get; set; } = false;

        [Timestamp]
        public uint Version { get; set; }

        public Card() { }

        public Card(string name, string description, int listId, EntityStatus status)
        {
            Name = name;
            Description = description;
            CardListId = listId;
            Status = status;
        }
    }
}