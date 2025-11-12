using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;

namespace FakeTrello.Service
{
    public class UserBoardService : IUserBoardService
    {
        private readonly IUserBoardRepository _userBoardRepository;

        public UserBoardService(IUserBoardRepository userBoardRepository)
        {
            _userBoardRepository = userBoardRepository;
        }

        public async Task<UserBoard?> GetByUserIdAndBoardId(int userId, int boardId)
        {
            return await _userBoardRepository.GetByUserAndBoardId(userId, boardId);
        }

        public async Task<UserBoard> Create(UserBoard userBoard)
        {
            return await _userBoardRepository.CreateAsync(userBoard);
        }

        public async Task<List<UserBoard>> GetAll()
        {
            return await _userBoardRepository.GetAll();
        }

        public async Task<UserBoard> GetByOwnerRoleAndBoardId(int boardId)
        {
            return await _userBoardRepository.GetByOwnerRoleAndBoardId(boardId);
        }

        public async Task<List<UserBoard>> GetAllByUserId(int userId)
        {
            return await _userBoardRepository.GetAllByUserId(userId);
        }

        public async Task<string> GetUsernameOfBoardOwner(int boardId)
        {
            return await _userBoardRepository.GetUsernameOfBoardOwner(boardId);
        }

        public async Task<UserBoard> Update(UserBoard userBoard)
        {
            return await _userBoardRepository.UpdateAsync(userBoard);
        }

        public async Task Delete(UserBoard userBoard)
        {
            await _userBoardRepository.DeleteAsync(userBoard.UserId, userBoard.BoardId);
        }
    }
}
