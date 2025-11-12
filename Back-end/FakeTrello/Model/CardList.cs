using FakeTrello.Model.Enum;

namespace FakeTrello.Model
{
    public class CardList
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BoardId { get; set; }
        public EntityStatus Status {  get; set; }
        public Board Board { get; set; }
        public int Index { get; set; }
        public ICollection<Card> Cards { get; set; }
        public CardList() { }
        public CardList(string name, EntityStatus status)
        {
            Name = name;
            Status = status;
        }
    }
}
