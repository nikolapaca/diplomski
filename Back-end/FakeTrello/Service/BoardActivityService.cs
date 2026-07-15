using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Hub;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.SignalR;

namespace FakeTrello.Service
{
    public class BoardActivityService : IBoardActivityService
    {
        private readonly IBoardActivityRepository _activityRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserBoardService _userBoardService;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public BoardActivityService(IBoardActivityRepository activityRepository, IBoardRepository boardRepository, IUserRepository userRepository, IUserBoardService userBoardService, IMapper mapper, IHubContext<NotificationHub> hubContext)
        {
            _activityRepository = activityRepository;
            _boardRepository = boardRepository;
            _userRepository = userRepository;
            _userBoardService = userBoardService;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        public async Task<Result<BoardActivityDTO>> Create(int boardId, int creatingUserId, ActivityType type, string message, int? cardId = null)
        {
            var user = await _userRepository.GetById(creatingUserId);

            if (user == null)
            {
                return Result.Fail("Creating user doesn't exist.");
            }

            var activity = new BoardActivity
            {
                BoardId = boardId,
                CreatingUserId = creatingUserId,
                CardId = cardId,
                Type = type,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _activityRepository.Create(activity);

            created.CreatingUser = user;

            var activityDto = _mapper.Map<BoardActivityDTO>(created);

            var board = await _boardRepository.GetById(boardId);
            if (board != null)
            {
                var ownerUsername = await _userBoardService.GetUsernameOfBoardOwner(boardId);
                if (!string.IsNullOrEmpty(ownerUsername))
                {
                    var groupName = NotificationHub.GetBoardGroupName(ownerUsername, board.Name);
                    await _hubContext.Clients.Group(groupName).SendAsync("ReceiveBoardActivity", activityDto);
                }
            }

            return Result.Ok(activityDto);
        }

        public async Task<Result<List<BoardActivityDTO>>> GetByBoard(string boardName, string ownerUsername)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(boardName, ownerUsername);

            if (board == null)
            {
                return Result.Fail("Board doesn't exist.");
            }

            var activities = await _activityRepository.GetByBoardId(board.Id);

            return Result.Ok(_mapper.Map<List<BoardActivityDTO>>(activities));
        }
    }
}