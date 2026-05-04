using FakeTrello.Model;

namespace FakeTrello.Repository.Contract
{
    public interface IUserBoardRepository
    {
        Task<List<UserBoard>> GetAll();
        Task<List<UserBoard>> GetAllByUserId(int userId);
        Task<string> GetUsernameOfBoardOwner(int boardId);
        Task<UserBoard> GetByOwnerRoleAndBoardId(int boardId);
        Task<UserBoard> CreateAsync(UserBoard userBoard);
        Task<UserBoard> UpdateAsync(UserBoard userBoard);
        Task<UserBoard?> GetByUserAndBoardId(int userId, int boardId);
        Task DeleteAsync(int userId, int boardId);
        void RemoveRange(IEnumerable<UserBoard> userBoards);
    }
}
