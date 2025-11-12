using FakeTrello.DTO;
using FakeTrello.Model;

namespace FakeTrello.Service.Contract
{
    public interface IUserBoardService
    {
        Task<List<UserBoard>> GetAll();
        Task<UserBoard> GetByOwnerRoleAndBoardId(int boardId);
        Task<string> GetUsernameOfBoardOwner(int boardId);
        Task<UserBoard?> GetByUserIdAndBoardId(int userId, int boardId);
        Task<List<UserBoard>> GetAllByUserId(int userId);
        Task<UserBoard> Create(UserBoard userBoard);
        Task<UserBoard> Update(UserBoard userBoard);
        Task Delete(UserBoard userBoard);
    }
}
