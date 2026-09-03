using Pivot.DTO;
using Pivot.Model;

namespace Pivot.Service.Contract
{
    public interface IUserBoardService
    {
        Task<List<UserBoard>> GetAll();
        Task<UserBoard> GetByOwnerRoleAndBoardId(int boardId);
        Task<string> GetUsernameOfBoardOwner(int boardId);
        Task<UserBoard?> GetByUserIdAndBoardId(int userId, int boardId);
        Task<List<UserBoard>> GetAllByUserId(int userId);
        Task<List<BoardMemberDTO>> GetMembers(int boardId);
        Task<UserBoard> Create(UserBoard userBoard);
        Task<bool> IsUserMemberOfBoard(string username, int boardId);
        Task<bool> IsUserOwnerOfBoard(string username, int boardId);
        Task<UserBoard> Update(UserBoard userBoard);
        Task Delete(UserBoard userBoard);
        void RemoveRange(IEnumerable<UserBoard> userBoards);
    }
}