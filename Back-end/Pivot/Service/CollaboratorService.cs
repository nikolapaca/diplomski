using AutoMapper;
using Pivot.DTO;
using Pivot.Model;
using Pivot.Repository.Contract;
using Pivot.Service.Contract;
using FluentResults;

namespace Pivot.Service
{
    public class CollaboratorService : ICollaboratorService
    {
        public readonly IUserRepository _userRepository;
        public readonly IBoardRepository _boardRepository;
        private readonly IMapper _mapper;

        public CollaboratorService(IUserRepository userRepository, IBoardRepository boardRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _boardRepository = boardRepository;
            _mapper = mapper;
        }

        public async Task<Result<List<UserDTO>>> GetUsersNotOnBoard(string searchTerm, string boardName, string boardOwnerUsername)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(boardName, boardOwnerUsername);
            if (board == null)
            {
                return Result.Fail("Board doesnt exist!");
            }
            var users = await _userRepository.GetUsersNotOnTheBoard(searchTerm, board.Id);
            return Result.Ok(_mapper.Map<List<User>, List<UserDTO>>(users));
        }

        public async Task<Result<List<UserDTO>>> GetUsersOnBoard(string searchTerm, string boardName, string boardOwnerUsername)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(boardName, boardOwnerUsername);
            if (board == null)
            {
                return Result.Fail("Board doesnt exist!");
            }
            var users = await _userRepository.GetUsersOnTheBoard(searchTerm, board.Id);
            return Result.Ok(_mapper.Map<List<User>, List<UserDTO>>(users));
        }

        public async Task<Result<List<UserDTO>>> GetAssignableUsersOnBoard(string searchTerm, string boardName, string boardOwnerUsername, int cardId)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(boardName, boardOwnerUsername);
            if (board == null)
            {
                return Result.Fail("Board doesnt exist!");
            }
            var users = await _userRepository.GetAssignableUsersOnTheBoard(searchTerm, board.Id, cardId);
            return Result.Ok(_mapper.Map<List<User>, List<UserDTO>>(users));
        }
    }
}
