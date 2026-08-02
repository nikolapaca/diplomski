using AutoMapper;
using FakeTrello.Data.Contract;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Service
{
    public class CardListService : ICardListService
    {
        private readonly ICardListRepository _cardListRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IBoardService _boardService;
        private readonly IUserService _userService;
        private readonly IUserBoardService _userBoardService;
        private readonly IBoardActivityService _boardActivityService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CardListService(ICardListRepository cardListRepository, ICardRepository cardRepository, IMapper mapper, IBoardService boardService, IUnitOfWork unitOfWork, IUserService userService, IUserBoardService userBoardService, IBoardActivityService boardActivityService)
        {
            _cardListRepository = cardListRepository;
            _cardRepository = cardRepository;
            _mapper = mapper;
            _boardService = boardService;
            _unitOfWork = unitOfWork;
            _userService = userService;
            _userBoardService = userBoardService;
            _boardActivityService = boardActivityService;
        }

        public async Task<Result<CardListDTO>> Create(CardListDTO cardListDTO, string username)
        {
            CardList cardList = _mapper.Map<CardListDTO, CardList>(cardListDTO);
            Board? board = await _boardService.GetByNameAndOwnerUsername(cardListDTO.BoardName, cardListDTO.BoardUsername);
            if (board == null)
            {
                return Result.Fail("Board doesn't exist!");
            }

            if (!await _userBoardService.IsUserMemberOfBoard(username, board.Id))
            {
                return Result.Fail("You don't have access to this board.");
            }

            cardList.BoardId = board.Id;
            var index = await _cardListRepository.GetMaxIndexForCardListAsync(board.Id);
            cardList.Index = index is null ? 1 : (int)index + 1;
            await _cardListRepository.Create(cardList);

            var user = await _userService.GetUserByUsername(username);
            if (user != null)
            {
                await _boardActivityService.Create(
                    boardId: board.Id,
                    creatingUserId: user.Id,
                    type: ActivityType.LIST_CREATED,
                    message: $"{user.Username} created list '{cardList.Name}'."
                );
            }

            return Result.Ok(_mapper.Map<CardList, CardListDTO>(cardList));
        }

        public async Task<Result<List<CardListDTO>>> GetAll()
        {
            return _mapper.Map<List<CardList>, List<CardListDTO>>(await _cardListRepository.GetAll());
        }

        public async Task<Result<CardListDTO>> GetById(int id)
        {
            var cardList = await _cardListRepository.GetById(id);
            if (cardList == null)
            {
                return Result.Fail("CardList with the given ID was not found.");
            }

            var cardListDto = _mapper.Map<CardList, CardListDTO>(cardList);

            var boardResult = await _boardService.GetById(cardList.BoardId);
            if (boardResult.IsSuccess)
            {
                cardListDto.BoardName = boardResult.Value.Name;
                cardListDto.BoardUsername = await _userBoardService.GetUsernameOfBoardOwner(cardList.BoardId);
            }

            return Result.Ok(cardListDto);
        }

        public async Task<int?> GetBoardIdByListId(int listId)
        {
            var cardList = await _cardListRepository.GetById(listId);
            return cardList?.BoardId;
        }

        public async Task<Result<List<CardListDTO>>> GetByBoardNameAndBoardOwner(string boardName, string boardOwnerUsername)
        {
            return _mapper.Map<List<CardList>, List<CardListDTO>>(await _cardListRepository.GetByBoardOwnerAndBoardName(boardName, boardOwnerUsername));
        }

        public async Task<Result<CardListDTO>> Update(CardListDTO cardListDTO, string username)
        {
            CardList cardList = await _cardListRepository.GetById(cardListDTO.Id);
            if (cardList == null)
            {
                return Result.Fail("CardList with the given ID was not found.");
            }

            if (!await _userBoardService.IsUserMemberOfBoard(username, cardList.BoardId))
            {
                return Result.Fail("You don't have access to this board.");
            }

            try
            {
                cardList.Name = cardListDTO.Name;
                var updatedCardList = await _cardListRepository.Update(cardList);

                if (updatedCardList == null)
                {
                    return Result.Fail("Failed to update the CardList.");
                }

                var user = await _userService.GetUserByUsername(username);
                if (user != null)
                {
                    await _boardActivityService.Create(
                        boardId: updatedCardList.BoardId,
                        creatingUserId: user.Id,
                        type: ActivityType.LIST_UPDATED,
                        message: $"{user.Username} renamed list to '{updatedCardList.Name}'."
                    );
                }

                return Result.Ok(_mapper.Map<CardList, CardListDTO>(updatedCardList));
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during update: {ex.Message}");
            }
        }

        public async Task<Result> Delete(int id, string username)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var getByIdResult = await GetById(id);

                if (!getByIdResult.IsSuccess)
                {
                    return Result.Fail(getByIdResult.Errors.First().Message);
                }

                var cardListEntity = await _cardListRepository.GetById(id);
                var boardId = cardListEntity.BoardId;
                var listName = getByIdResult.Value.Name;

                if (!await _userBoardService.IsUserMemberOfBoard(username, boardId))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("You don't have access to this board.");
                }

                foreach (var card in getByIdResult.Value.Cards)
                {
                    await _cardRepository.Delete(card.Id);
                }
                await _cardListRepository.Delete(id);

                var user = await _userService.GetUserByUsername(username);
                if (user != null)
                {
                    await _boardActivityService.Create(
                        boardId: boardId,
                        creatingUserId: user.Id,
                        type: ActivityType.LIST_DELETED,
                        message: $"{user.Username} deleted list '{listName}'."
                    );
                }

                await _unitOfWork.CommitAsync();
                return Result.Ok();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("Failed to delete this card.");
            }
        }

        public async Task<Result> MoveList(CardListDTO cardList, int targetIndex, string username)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var list = await _cardListRepository.GetById(cardList.Id);
                if (list == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("List not found.");
                }

                if (!await _userBoardService.IsUserMemberOfBoard(username, list.BoardId))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("You don't have access to this board.");
                }

                var lists = await _cardListRepository.GetByBoardId(list.BoardId);

                int oldIndex = list.Index;

                if (oldIndex == targetIndex)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Ok();
                }

                if (oldIndex < targetIndex)
                {
                    foreach (var l in lists.Where(l => l.Id != list.Id && l.Index > oldIndex && l.Index <= targetIndex))
                        l.Index--;
                }
                else
                {
                    foreach (var l in lists.Where(l => l.Id != list.Id && l.Index >= targetIndex && l.Index < oldIndex))
                        l.Index++;
                }

                list.Index = targetIndex;

                await _cardListRepository.UpdateRangeAsync(lists);

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("This board's lists were changed by someone else while you were reordering. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred during list reorder: {ex.Message}");
            }
        }

        public async Task<Result> TogglePin(int listId, string username)
        {
            var list = await _cardListRepository.GetById(listId);
            if (list == null)
            {
                return Result.Fail("CardList with the given ID was not found.");
            }

            if (!await _userBoardService.IsUserOwnerOfBoard(username, list.BoardId))
            {
                return Result.Fail("Only the board owner can pin or unpin lists.");
            }

            list.IsPinned = !list.IsPinned;
            var updatedList = await _cardListRepository.Update(list);

            var boardLists = await _cardListRepository.GetByBoardId(updatedList.BoardId);
            for (int i = 0; i < boardLists.Count; i++)
            {
                boardLists[i].Index = i + 1;
            }
            await _cardListRepository.UpdateRangeAsync(boardLists);

            var user = await _userService.GetUserByUsername(username);
            if (user != null)
            {
                await _boardActivityService.Create(
                    boardId: updatedList.BoardId,
                    creatingUserId: user.Id,
                    type: updatedList.IsPinned ? ActivityType.LIST_PINNED : ActivityType.LIST_UNPINNED,
                    message: $"{user.Username} {(updatedList.IsPinned ? "pinned" : "unpinned")} list '{updatedList.Name}'."
                );
            }

            return Result.Ok();
        }
    }
}