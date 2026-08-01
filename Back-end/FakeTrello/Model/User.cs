using FakeTrello.Model.Enum;
using System.Globalization;

namespace FakeTrello.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public EntityStatus Status { get; set; }
        public List<UserBoard> UserBoards { get; set; } = new List<UserBoard>();
        public ICollection<Card> Cards { get; set; } = new List<Card>();
        public bool EmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiration { get; set; }
        public User() { }

        public User(int id, string name, string surname, string email, string username, string password, EntityStatus status)
        {
            Id = id;
            Name = name;
            Surname = surname;
            Email = email;
            Username = username;
            Password = password;
            Status = status;
        }

    }
}
