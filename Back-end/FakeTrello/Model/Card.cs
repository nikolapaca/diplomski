using FakeTrello.Model.Enum;

namespace FakeTrello.Model
{
    public class Card
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CardListId {  get; set; }
        public CardList CardList {  get; set; }
        public int? UserId { get; set; }
        public int Index { get; set; }
        public User User { get; set;}
        public EntityStatus Status {  get; set; }

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
