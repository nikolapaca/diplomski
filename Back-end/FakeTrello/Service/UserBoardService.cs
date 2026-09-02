using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;

namespace FakeTrello.Service
{
    public class UserBoardService : IUserBoardService
    {
        private readonly IUserBoardRepository _userBoardRepository;
        private readonly IUserRepository _userRepository;

        public UserBoardService(IUserBoardRepository userBoardRepository, IUserRepository userRepository)
        {
            _userBoardRepository = userBoardRepository;
            _userRepository = userRepository;
        }

        public async Task<UserBoard?> GetByUserIdAndBoardId(int userId, int boardId)
        {
            return await _userBoardRepository.GetByUserAndBoardId(userId, boardId);
        }

        public async Task<bool> IsUserMemberOfBoard(string username, int boardId)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            var user = await _userRepository.GetByUsername(username);
            if (user == null)
            {
                return false;
            }

            var membership = await _userBoardRepository.GetByUserAndBoardId(user.Id, boardId);
            return membership != null;
        }

        public async Task<bool> IsUserOwnerOfBoard(string username, int boardId)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            var user = await _userRepository.GetByUsername(username);
            if (user == null)
            {
                return false;
            }

            var membership = await _userBoardRepository.GetByUserAndBoardId(user.Id, boardId);
            return membership != null && membership.UserRole == UserRole.OWNER;
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

        public async Task<List<BoardMemberDTO>> GetMembers(int boardId)
        {
            var userBoards = await _userBoardRepository.GetByBoardId(boardId);

            return userBoards.Select(ub => new BoardMemberDTO
            {
                Username = ub.User.Username,
                Name = ub.User.Name,
                Surname = ub.User.Surname,
                Role = ub.UserRole
            }).ToList();
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

        public void RemoveRange(IEnumerable<UserBoard> userBoards)
        {
            _userBoardRepository.RemoveRange(userBoards);
        }
    }
}