using AutoMapper;
using FakeTrello.Data.Contract;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;

namespace FakeTrello.Service
{
    public class BoardService : IBoardService
    {
        private readonly IBoardRepository _boardRepository;
        private readonly IUserService _userService;
        private readonly IUserBoardService _userBoardService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IBoardActivityService _boardActivityService;
        private readonly ICardRepository _cardRepository;
        private readonly ICardListRepository _cardListRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<BoardService> _logger;

        public BoardService(IBoardRepository boardRepository, IMapper mapper, IUserService userService, IUserBoardService userBoardService, IUnitOfWork uow, INotificationService notificationService, IBoardActivityService boardActivityService, ICardRepository cardRepository, ICardListRepository cardListRepository, ILogger<BoardService> logger)
        {
            _boardRepository = boardRepository;
            _mapper = mapper;
            _userService = userService;
            _userBoardService = userBoardService;
            _unitOfWork = uow;
            _notificationService = notificationService;
            _boardActivityService = boardActivityService;
            _cardRepository = cardRepository;
            _cardListRepository = cardListRepository;
            _logger = logger;
        }

        public async Task<Result<BoardDTO>> Create(BoardDTO boardDto, string username)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await GetCurrentUser(username);

                var existingBoards = await _boardRepository.GetByName(boardDto.Name);

                if (existingBoards.Count > 0)
                {
                    foreach (var varBoard in existingBoards)
                    {
                        var thisBoard = await _userBoardService.GetByOwnerRoleAndBoardId(varBoard.Id);
                        if (thisBoard.UserId == user.Id)
                        {
                            await _unitOfWork.RollbackAsync();
                            return Result.Fail("Board with this name already exists for this user!");
                        }
                    }
                }

                var board = _mapper.Map<BoardDTO, Board>(boardDto);
                await _boardRepository.Create(board);
                var userBoard = new UserBoard(user.Id, board.Id, UserRole.OWNER);
                await _userBoardService.Create(userBoard);

                await _unitOfWork.CommitAsync();

                return Result.Ok(_mapper.Map<Board, BoardDTO>(board));
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("Failed to create the board.");
            }
        }

        public async Task<Result<BoardDTO>> Update(BoardUpdateDTO boardDto, string requestingUsername)
        {
            Board? board = await GetByNameAndOwnerUsername(boardDto.OldBoardName, boardDto.OwnerUsername);
            if (board == null)
            {
                return Result.Fail("No board found!");
            }

            var isOwner = await _userBoardService.IsUserOwnerOfBoard(requestingUsername, board.Id);

            if (!isOwner && !await _userBoardService.IsUserMemberOfBoard(requestingUsername, board.Id))
            {
                return Result.Fail("You don't have access to this board.");
            }

            var isRenaming = !string.Equals(boardDto.NewBoardName, boardDto.OldBoardName, StringComparison.Ordinal);

            if (isRenaming && !isOwner)
            {
                return Result.Fail("Only the board owner can rename the board.");
            }

            if (isRenaming)
            {
                board.Name = boardDto.NewBoardName;
            }

            board.Description = boardDto.NewBoardDescription;
            Board updatedBoard = await _boardRepository.Update(board);

            var requestingUser = await _userService.GetUserByUsername(requestingUsername);
            if (requestingUser != null)
            {
                await _boardActivityService.Create(
                    boardId: updatedBoard.Id,
                    creatingUserId: requestingUser.Id,
                    type: ActivityType.BOARD_UPDATED,
                    message: isRenaming
                        ? $"{requestingUser.Username} updated board '{updatedBoard.Name}'."
                        : $"{requestingUser.Username} updated the board description."
                );
            }

            return Result.Ok(_mapper.Map<Board, BoardDTO>(updatedBoard));
        }

        public async Task<Result> Delete(string name, string username, string requestingUsername)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var board = await GetByNameAndOwnerUsername(name, username);
                if (board == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This board doesn't exist!");
                }

                if (!await _userBoardService.IsUserOwnerOfBoard(requestingUsername, board.Id))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Only the board owner can delete this board.");
                }

                var owner = await _userService.GetUserByUsername(username);
                if (owner == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Owner doesn't exist!");
                }

                var boardMembers = board.UserBoards.ToList();

                foreach (var member in boardMembers)
                {
                    if (member.UserId == owner.Id)
                        continue;

                    try
                    {
                        await _notificationService.Create(
                            recipientUserId: member.UserId,
                            creatingUserId: owner.Id,
                            type: NotificationType.BOARD_DELETED,
                            message: $"Board '{board.Name}' has been deleted.",
                            boardId: board.Id
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to notify user {UserId} while deleting board {BoardId}.", member.UserId, board.Id);
                    }
                }

                _userBoardService.RemoveRange(board.UserBoards);
                await _unitOfWork.SaveChangesAsync();

                var cardIds = board.Lists.SelectMany(l => l.Cards).Select(c => c.Id).ToList();
                var listIds = board.Lists.Select(l => l.Id).ToList();

                await _cardRepository.DeleteRange(cardIds);
                await _cardListRepository.DeleteRange(listIds);

                await _boardRepository.Delete(board.Id);

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("Failed to delete the board.");
            }
        }

        public async Task<Result<List<BoardDTO>>> GetAllByUsername(string username)
        {
            var user = await GetCurrentUser(username);
            var boards = await _boardRepository.GetAllByUserIdOrdered(user);
            var boardsDto = new List<BoardDTO>();
            foreach (var board in boards)
            {
                var boardDto = _mapper.Map<Board, BoardDTO>(board);
                boardDto.OwnerUsername = await _userBoardService.GetUsernameOfBoardOwner(board.Id);
                var membership = await _userBoardService.GetByUserIdAndBoardId(user.Id, board.Id);
                boardDto.IsFavorite = membership?.IsFavorite ?? false;
                boardsDto.Add(boardDto);
            }
            return boardsDto;
        }

        public async Task<Result<List<BoardDTO>>> GetAllArchivedByUsername(string username)
        {
            var user = await GetCurrentUser(username);
            var boards = await _boardRepository.GetAllArchivedOwnedByUser(user);
            var boardsDto = new List<BoardDTO>();
            foreach (var board in boards)
            {
                var boardDto = _mapper.Map<Board, BoardDTO>(board);
                boardDto.OwnerUsername = username;
                boardsDto.Add(boardDto);
            }
            return boardsDto;
        }

        public async Task<Result<List<BoardDTO>>> GetBySearchFilter(string username, string searchQuery)
        {
            var user = await GetCurrentUser(username);
            var boards = await _boardRepository.GetBySearchFilter(user.Id, searchQuery);
            var boardsDto = new List<BoardDTO>();
            foreach (var board in boards)
            {
                var boardDto = _mapper.Map<Board, BoardDTO>(board);
                boardDto.OwnerUsername = await _userBoardService.GetUsernameOfBoardOwner(board.Id);
                var membership = await _userBoardService.GetByUserIdAndBoardId(user.Id, board.Id);
                boardDto.IsFavorite = membership?.IsFavorite ?? false;
                boardsDto.Add(boardDto);
            }
            return boardsDto;
        }

        private async Task<User> GetCurrentUser(string username)
        {
            var user = await _userService.GetUserByUsername(username);
            return user;
        }

        public async Task<Result<BoardDTO>> GetById(int id)
        {
            var board = await _boardRepository.GetById(id);
            if (board == null)
                return Result.Fail("This board doesn't exist!");
            var boardDto = _mapper.Map<Board, BoardDTO>(board);
            return Result.Ok(boardDto);
        }

        public async Task<Board?> GetByNameAndOwnerUsername(string name, string username)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(name, username);
            return board;
        }

        public async Task<Result<BoardDTO>> GetResultByNameAndOwnerUsername(string name, string username, string? requestingUsername = null)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(name, username);
            if (board == null)
            {
                return Result.Fail("This board doesnt exist!");
            }
            var boardDTO = _mapper.Map<Board, BoardDTO>(board);
            boardDTO.OwnerUsername = username;

            if (!string.IsNullOrEmpty(requestingUsername))
            {
                var requestingUser = await _userService.GetUserByUsername(requestingUsername);
                if (requestingUser != null)
                {
                    var membership = await _userBoardService.GetByUserIdAndBoardId(requestingUser.Id, board.Id);
                    boardDTO.IsFavorite = membership?.IsFavorite ?? false;
                }
            }

            return Result.Ok(boardDTO);
        }

        public async Task<Result> AddCollaboratorToBoard(BoardDTO boardDto, string username, string requestingUsername)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var board = await _boardRepository.GetByNameAndOwnerUsername(boardDto.Name, boardDto.OwnerUsername);
                if (board == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This board doesnt exist!");
                }

                if (!await _userBoardService.IsUserOwnerOfBoard(requestingUsername, board.Id))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Only the board owner can add collaborators.");
                }

                var user = await _userService.GetUserByUsername(username);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user doesnt exist!");
                }

                var owner = await _userService.GetUserByUsername(boardDto.OwnerUsername);
                if (owner == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Owner doesnt exist!");
                }

                var existingMembership = await _userBoardService.GetByUserIdAndBoardId(user.Id, board.Id);
                if (existingMembership != null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail($"{user.Username} is already on this board.");
                }

                UserBoard ub = new UserBoard(user.Id, board.Id, UserRole.COLLABORATOR);
                await _userBoardService.Create(ub);

                try
                {
                    await _notificationService.Create(
                        recipientUserId: user.Id,
                        creatingUserId: owner.Id,
                        type: NotificationType.ADDED_TO_BOARD,
                        message: $"{owner.Username} added you to board '{board.Name}'.",
                        boardId: board.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify user {UserId} about being added to board {BoardId}.", user.Id, board.Id);
                }

                await _boardActivityService.Create(
                    boardId: board.Id,
                    creatingUserId: owner.Id,
                    type: ActivityType.COLLABORATOR_ADDED,
                    message: $"{owner.Username} added {user.Username} to the board."
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred while adding collaborator: {ex.Message}");
            }
        }

        public async Task<Result> RemoveCollaboratorFromBoard(BoardDTO boardDto, string username, string requestingUsername)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var board = await _boardRepository.GetByNameAndOwnerUsername(boardDto.Name, boardDto.OwnerUsername);
                if (board == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This board doesnt exist!");
                }

                if (!await _userBoardService.IsUserOwnerOfBoard(requestingUsername, board.Id))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Only the board owner can remove collaborators.");
                }

                var user = await _userService.GetUserByUsername(username);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user doesnt exist!");
                }

                var owner = await _userService.GetUserByUsername(boardDto.OwnerUsername);
                if (owner == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Owner doesnt exist!");
                }

                UserBoard ub = await _userBoardService.GetByUserIdAndBoardId(user.Id, board.Id);
                if (ub == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user is not collaborator on this board!");
                }

                await _userBoardService.Delete(ub);

                try
                {
                    await _notificationService.Create(
                        recipientUserId: user.Id,
                        creatingUserId: owner.Id,
                        type: NotificationType.REMOVED_FROM_BOARD,
                        message: $"{owner.Username} removed you from board '{board.Name}'.",
                        boardId: board.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify user {UserId} about being removed from board {BoardId}.", user.Id, board.Id);
                }

                await _boardActivityService.Create(
                    boardId: board.Id,
                    creatingUserId: owner.Id,
                    type: ActivityType.COLLABORATOR_REMOVED,
                    message: $"{owner.Username} removed {user.Username} from the board."
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred while removing collaborator: {ex.Message}");
            }
        }

        public async Task<Result> LeaveBoard(BoardDTO boardDto, string username)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var board = await _boardRepository.GetByNameAndOwnerUsername(
                    boardDto.Name,
                    boardDto.OwnerUsername
                );

                if (board == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This board doesnt exist!");
                }

                var user = await _userService.GetUserByUsername(username);

                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user doesnt exist!");
                }

                var userBoard = await _userBoardService.GetByUserIdAndBoardId(user.Id, board.Id);

                if (userBoard == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("User is not on this board!");
                }

                if (userBoard.UserRole == UserRole.OWNER)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Owner cannot leave his own board!");
                }

                await _userBoardService.Delete(userBoard);

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"Failed to leave board: {ex.Message}");
            }
        }

        public async Task<Result> ToggleFavorite(string boardName, string boardOwnerUsername, string username)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(boardName, boardOwnerUsername);
            if (board == null)
            {
                return Result.Fail("This board doesn't exist!");
            }

            var user = await _userService.GetUserByUsername(username);
            if (user == null)
            {
                return Result.Fail("This user doesn't exist!");
            }

            var membership = await _userBoardService.GetByUserIdAndBoardId(user.Id, board.Id);
            if (membership == null)
            {
                return Result.Fail("You don't have access to this board.");
            }

            membership.IsFavorite = !membership.IsFavorite;
            await _userBoardService.Update(membership);

            return Result.Ok();
        }

        public async Task<Result> Archive(string name, string ownerUsername, string requestingUsername)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var board = await _boardRepository.GetByNameAndOwnerUsername(name, ownerUsername);
                if (board == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This board doesn't exist!");
                }

                if (!await _userBoardService.IsUserOwnerOfBoard(requestingUsername, board.Id))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Only the owner can archive this board.");
                }

                var owner = await _userService.GetUserByUsername(requestingUsername);
                if (owner == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Owner doesn't exist!");
                }

                board.Status = BoardStatus.ARCHIVED;
                await _boardRepository.Update(board);

                foreach (var member in board.UserBoards)
                {
                    if (member.UserId == owner.Id)
                        continue;

                    try
                    {
                        await _notificationService.Create(
                            recipientUserId: member.UserId,
                            creatingUserId: owner.Id,
                            type: NotificationType.BOARD_ARCHIVED,
                            message: $"Board '{board.Name}' has been archived.",
                            boardId: board.Id
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to notify user {UserId} while archiving board {BoardId}.", member.UserId, board.Id);
                    }
                }

                await _boardActivityService.Create(
                    boardId: board.Id,
                    creatingUserId: owner.Id,
                    type: ActivityType.BOARD_ARCHIVED,
                    message: $"{owner.Username} archived board '{board.Name}'."
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"Failed to archive the board: {ex.Message}");
            }
        }

        public async Task<Result> Unarchive(string name, string ownerUsername, string requestingUsername)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var board = await _boardRepository.GetArchivedByNameAndOwnerUsername(name, ownerUsername);
                if (board == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This archived board doesn't exist!");
                }

                if (!await _userBoardService.IsUserOwnerOfBoard(requestingUsername, board.Id))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Only the owner can unarchive this board.");
                }

                var owner = await _userService.GetUserByUsername(requestingUsername);
                if (owner == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Owner doesn't exist!");
                }

                board.Status = BoardStatus.ACTIVE;
                await _boardRepository.Update(board);

                await _boardActivityService.Create(
                    boardId: board.Id,
                    creatingUserId: owner.Id,
                    type: ActivityType.BOARD_UPDATED,
                    message: $"{owner.Username} restored board '{board.Name}' from the archive."
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"Failed to unarchive the board: {ex.Message}");
            }
        }
    }
}