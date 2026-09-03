using Pivot.Model;

namespace Pivot.Repository.Contract
{
    public interface IBoardActivityRepository
    {
        Task<BoardActivity> Create(BoardActivity activity);
        Task<List<BoardActivity>> GetByBoardId(int boardId);
        Task<List<BoardActivity>> GetByCardId(int cardId);
    }
}