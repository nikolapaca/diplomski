using System.ComponentModel.DataAnnotations.Schema;

namespace FakeTrello.Model
{
    public enum BoardStatus
    {
        ACTIVE,
        ARCHIVED,
        DELETED
    }
    
    public class Board
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public BoardStatus Status { get; set; }
        public ICollection<UserBoard> UserBoards { get; set; } = new List<UserBoard>();
        public ICollection<CardList> Lists { get; set; } = new List<CardList>();

        public Board() { }

        public Board(int id, string name, string description, BoardStatus status)
        {
            Id = id;
            Name = name;
            Description = description;
            Status = status;
        }
    }
}
