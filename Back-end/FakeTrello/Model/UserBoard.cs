using FakeTrello.Model.Enum;

namespace FakeTrello.Model
{
    public enum UserRole
    {
        OWNER,
        COLLABORATOR
    }

    public class UserBoard
    {
        public int UserId { get; set; }
        public int BoardId { get; set; }

        public User User { get; set; }
        public Board Board { get; set; }
        public UserRole UserRole { get; set; }
        public ICollection<CardAssignee> AssignedCards { get; set; } = new List<CardAssignee>();
        public UserBoard() { }

        public UserBoard(int userId, int boardId, UserRole userRole)
        {
            UserId = userId;
            BoardId = boardId;
            UserRole = userRole;
        }
    }
}
