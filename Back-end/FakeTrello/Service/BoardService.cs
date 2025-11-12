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
        private readonly IMapper _mapper;

        public BoardService(IBoardRepository boardRepository, IMapper mapper, IUserService userService, IUserBoardService userBoardService, IUnitOfWork uow)
        {
            _boardRepository = boardRepository;
            _mapper = mapper;
            _userService = userService;
            _userBoardService = userBoardService;
            _unitOfWork = uow;
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
                await _unitOfWork.Boards.Create(board);
                await _unitOfWork.SaveChangesAsync();
                var userBoard = new UserBoard(user.Id, board.Id, UserRole.OWNER);
                await _unitOfWork.UserBoards.CreateAsync(userBoard);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                return Result.Ok(_mapper.Map<Board, BoardDTO>(board));
            }
            catch(Exception) {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("Failed to create the board.");
            }
        }

        public async Task<Result<BoardDTO>> Update(BoardUpdateDTO boardDto)
        {
            Board? board = await GetByNameAndOwnerUsername(boardDto.OldBoardName, boardDto.OwnerUsername);
            if(board == null)
            {
                return Result.Fail("No board found!");
            }
            board.Name = boardDto.NewBoardName;
            board.Description = boardDto.NewBoardDescription;
            Board updatedBoard = await _boardRepository.Update(board);
            return Result.Ok(_mapper.Map<Board, BoardDTO>(updatedBoard));
        }

        public async Task<Result> Delete(string name, string username)
        {
            await _unitOfWork.BeginTransactionAsync();
            try 
            {
                var board = await GetByNameAndOwnerUsername(name, username);
                if(board == null)
                {
                    return Result.Fail("This board doesn't exist!");
                }
                foreach(var ub in board.UserBoards)
                {
                    await _unitOfWork.UserBoards.DeleteAsync(ub.UserId, ub.BoardId);
                }
                foreach(var list in board.Lists)
                {
                    foreach(var card in list.Cards)
                    {
                        await _unitOfWork.Cards.Delete(card.Id);
                    }

                    await _unitOfWork.CardLists.Delete(list.Id);
                }
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.Boards.Delete(board.Id);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();
                return Result.Ok();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("Failed to delete the board.");
            }
        }

        public async Task< Result<List<BoardDTO>>> GetAllByUsername(string username)
        {
            var user = await GetCurrentUser(username);
            var boards = await _boardRepository.GetAllByUserIdOrdered(user);
            var boardsDto = new List<BoardDTO>();
            foreach(var board in boards)
            {
                var boardDto = _mapper.Map<Board, BoardDTO>(board);
                boardDto.OwnerUsername = await _userBoardService.GetUsernameOfBoardOwner(board.Id);
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

        public async Task<Result<BoardDTO>> GetResultByNameAndOwnerUsername(string name, string username)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(name, username);
            if(board == null)
            {
                return Result.Fail("This board doesnt exist!");
            }
            var boardDTO = _mapper.Map<Board, BoardDTO>(board);
            boardDTO.OwnerUsername = username;
            return Result.Ok(boardDTO);
        }

        public async Task<Result> AddCollaboratorToBoard(BoardDTO boardDto, string username)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(boardDto.Name, boardDto.OwnerUsername);
            if (board == null)
            {
                return Result.Fail("This board doesnt exist!");
            }
            var user = await _userService.GetUserByUsername(username);
            if(user == null)
            {
                return Result.Fail("This user doesnt exist!");
            }
            UserBoard ub = new UserBoard(user.Id, board.Id, UserRole.COLLABORATOR);
            await _userBoardService.Create(ub);
            return Result.Ok();
        }

        public async Task<Result> RemoveCollaboratorFromBoard(BoardDTO boardDto, string username)
        {
            var board = await _boardRepository.GetByNameAndOwnerUsername(boardDto.Name, boardDto.OwnerUsername);
            if (board == null)
            {
                return Result.Fail("This board doesnt exist!");
            }
            var user = await _userService.GetUserByUsername(username);
            if (user == null)
            {
                return Result.Fail("This user doesnt exist!");
            }
            UserBoard ub = await _userBoardService.GetByUserIdAndBoardId(user.Id, board.Id);
            await _userBoardService.Delete(ub);
            return Result.Ok();
        }
    }
}
