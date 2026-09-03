namespace Pivot.Model
{
    public class CardAssignee
    {
        public int CardId { get; set; }
        public int UserId { get; set; }
        public int BoardId { get; set; }

        public Card Card { get; set; }
        public UserBoard UserBoard { get; set; }

        public CardAssignee() { }

        public CardAssignee(int cardId, int userId, int boardId)
        {
            CardId = cardId;
            UserId = userId;
            BoardId = boardId;
        }
    }
}
