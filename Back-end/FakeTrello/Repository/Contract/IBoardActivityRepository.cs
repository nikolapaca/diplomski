using FakeTrello.Model;

namespace FakeTrello.Repository.Contract
{
    public interface IBoardActivityRepository
    {
        Task<BoardActivity> Create(BoardActivity activity);
        Task<List<BoardActivity>> GetByBoardId(int boardId);
    }
}