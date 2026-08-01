using AutoMapper;
using FakeTrello.Data.Contract;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;

namespace FakeTrello.Service
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _cardRepository;
        private readonly ICardListService _cardListService;
        private readonly IUserService _userService;
        private readonly ICardAssigneeService _cardAssigneeService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IBoardActivityService _boardActivityService;
        private readonly IBoardService _boardService;
        private readonly IUserBoardService _userBoardService;

        public CardService(
            ICardRepository cardRepository,
            IMapper mapper,
            ICardListService cardListService,
            IUserService userService,
            ICardAssigneeService cardAssigneeService,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IBoardActivityService boardActivityService,
            IBoardService boardService,
            IUserBoardService userBoardService)
        {
            _cardRepository = cardRepository;
            _mapper = mapper;
            _cardListService = cardListService;
            _userService = userService;
            _cardAssigneeService = cardAssigneeService;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _boardActivityService = boardActivityService;
            _boardService = boardService;
            _userBoardService = userBoardService;
        }

        public async Task<Result<CardDTO>> Create(int cardListId, CardDTO cardDTO, int userId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var listResult = await _cardListService.GetById(cardListId);
                if (listResult.IsFailed)
                {
                    await _unitOfWork.RollbackAsync();
                    return listResult.ToResult<CardDTO>();
                }

                var userResult = await _userService.GetById(userId);
                if (userResult.IsFailed)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("User doesn't exist.");
                }

                CardListDTO list = listResult.Value;

                var board = await _boardService.GetByNameAndOwnerUsername(
                    list.BoardName,
                    list.BoardUsername
                );

                if (board == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Board doesn't exist.");
                }

                if (!await _userBoardService.IsUserMemberOfBoard(userResult.Value.Username, board.Id))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("You don't have access to this board.");
                }

                Card card = _mapper.Map<CardDTO, Card>(cardDTO);
                card.CardListId = list.Id;
                card.Status = EntityStatus.ACTIVE;

                var maxIndex = await _cardRepository.GetMaxIndexForCardAsync(list.Id);
                card.Index = (maxIndex ?? 0) + 1;
                card.CreatedByUserId = userId;

                await _unitOfWork.Cards.Create(card);
                await _unitOfWork.SaveChangesAsync();

                await _boardActivityService.Create(
                    boardId: board.Id,
                    creatingUserId: userId,
                    type: ActivityType.CARD_CREATED,
                    message: $"{userResult.Value.Username} created card '{card.Name}'.",
                    cardId: card.Id
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok(_mapper.Map<Card, CardDTO>(card));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred during card creation: {ex.Message}");
            }
        }

        public async Task<Result> ReorderCardInsideList(int cardId, int newIndex, string username)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userService.GetUserByUsername(username);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user doesn't exist!");
                }

                var card = await _cardRepository.GetById(cardId);
                if (card == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Card not found.");
                }

                if (!await _userBoardService.IsUserMemberOfBoard(username, card.CardList.BoardId))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("You don't have access to this board.");
                }

                var cards = await _cardRepository.GetByListId(card.CardListId);

                int oldIndex = card.Index;
                newIndex = Math.Clamp(newIndex, 1, cards.Count);

                if (oldIndex == newIndex)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Ok();
                }

                if (oldIndex < newIndex)
                {
                    foreach (var c in cards.Where(c => c.Id != card.Id && c.Index > oldIndex && c.Index <= newIndex))
                        c.Index--;
                }
                else
                {
                    foreach (var c in cards.Where(c => c.Id != card.Id && c.Index >= newIndex && c.Index < oldIndex))
                        c.Index++;
                }

                card.Index = newIndex;

                await _unitOfWork.Cards.UpdateRangeAsync(cards);
                await _unitOfWork.SaveChangesAsync();

                await _boardActivityService.Create(
                    boardId: card.CardList.BoardId,
                    creatingUserId: user.Id,
                    type: ActivityType.CARD_REORDERED,
                    message: $"{user.Username} reordered card '{card.Name}' in list '{card.CardList.Name}'.",
                    cardId: card.Id
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred during card reorder: {ex.Message}");
            }
        }

        public async Task<Result> ReorderCardOutsideList(int cardId, int targetListId, int targetIndex, string username)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userService.GetUserByUsername(username);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user doesn't exist!");
                }

                var card = await _cardRepository.GetById(cardId);
                if (card == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Card not found.");
                }

                int oldListId = card.CardListId;
                string oldListName = card.CardList.Name;
                int boardId = card.CardList.BoardId;

                if (!await _userBoardService.IsUserMemberOfBoard(username, boardId))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("You don't have access to this board.");
                }

                var targetListResult = await _cardListService.GetById(targetListId);
                if (targetListResult.IsFailed)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Target list doesn't exist.");
                }

                var targetListBoardId = await _cardListService.GetBoardIdByListId(targetListId);
                if (targetListBoardId == null || targetListBoardId != boardId)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Cannot move a card to a list on a different board.");
                }

                string targetListName = targetListResult.Value.Name;

                var oldListCards = (await _cardRepository.GetByListId(oldListId))
                    .Where(c => c.Id != card.Id)
                    .OrderBy(c => c.Index)
                    .ToList();

                var targetListCards = (await _cardRepository.GetByListId(targetListId))
                    .Where(c => c.Id != card.Id)
                    .OrderBy(c => c.Index)
                    .ToList();

                targetIndex = Math.Clamp(targetIndex, 1, targetListCards.Count + 1);

                for (int i = 0; i < oldListCards.Count; i++)
                    oldListCards[i].Index = i + 1;

                foreach (var c in targetListCards.Where(c => c.Index >= targetIndex))
                    c.Index++;

                card.CardListId = targetListId;
                card.Index = targetIndex;

                var cardsToUpdate = oldListCards
                    .Concat(targetListCards)
                    .Append(card)
                    .ToList();

                await _unitOfWork.Cards.UpdateRangeAsync(cardsToUpdate);
                await _unitOfWork.SaveChangesAsync();

                await _boardActivityService.Create(
                    boardId: boardId,
                    creatingUserId: user.Id,
                    type: ActivityType.CARD_MOVED,
                    message: $"{user.Username} moved card '{card.Name}' from '{oldListName}' to '{targetListName}'.",
                    cardId: card.Id
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred during card move: {ex.Message}");
            }
        }

        public async Task<Result<List<CardDTO>>> GetAll()
        {
            return _mapper.Map<List<Card>, List<CardDTO>>(await _cardRepository.GetAll());
        }

        public async Task<Result<CardDTO>> GetById(int id)
        {
            return _mapper.Map<Card?, CardDTO>(await _cardRepository.GetById(id));
        }

        public async Task<Result<List<CardDTO>>> GetByListId(int listId)
        {
            var cardsDTO = new List<CardDTO>();
            var cards = await _cardRepository.GetByListId(listId);

            foreach (var card in cards)
            {
                var cardDTO = _mapper.Map<Card, CardDTO>(card);
                cardDTO.AssignedUserUsernames = card.Assignees.Select(a => a.UserBoard.User.Username).ToList();
                cardsDTO.Add(cardDTO);
            }

            return cardsDTO;
        }

        public async Task<Result> AssignCardToUser(CardDTO cardDto, string username, string creatingUserUsername)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userService.GetUserByUsername(username);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user doesn't exist!");
                }

                var creatingUser = await _userService.GetUserByUsername(creatingUserUsername);
                if (creatingUser == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Creating user doesn't exist!");
                }

                var card = await _cardRepository.GetById(cardDto.Id);
                if (card == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This card doesn't exist!");
                }

                var boardId = card.CardList.BoardId;

                if (!await _userBoardService.IsUserMemberOfBoard(creatingUserUsername, boardId))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("You don't have access to this board.");
                }

                if (!await _userBoardService.IsUserMemberOfBoard(username, boardId))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail($"{user.Username} is not a member of this board.");
                }

                var cardAssignee = new CardAssignee(card.Id, user.Id, boardId);
                await _unitOfWork.CardAssignees.CreateAsync(cardAssignee);
                await _unitOfWork.SaveChangesAsync();

                if (user.Id != creatingUser.Id)
                {
                    await _notificationService.Create(
                        recipientUserId: user.Id,
                        creatingUserId: creatingUser.Id,
                        type: NotificationType.ASSIGNED_TO_CARD,
                        message: $"{creatingUser.Username} assigned you to card '{card.Name}'.",
                        boardId: boardId,
                        cardId: card.Id
                    );
                }

                await _boardActivityService.Create(
                    boardId: boardId,
                    creatingUserId: creatingUser.Id,
                    type: ActivityType.USER_ASSIGNED_TO_CARD,
                    message: $"{creatingUser.Username} assigned {user.Username} to card '{card.Name}'.",
                    cardId: card.Id
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred during assignment: {ex.Message}");
            }
        }

        public async Task<Result> UnassignCardToUser(CardDTO cardDto, string username, string unassigningUserUsername)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userService.GetUserByUsername(username);
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user doesn't exist!");
                }

                var unassigningUser = await _userService.GetUserByUsername(unassigningUserUsername);
                if (unassigningUser == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Unassigning user doesn't exist!");
                }

                var card = await _cardRepository.GetById(cardDto.Id);
                if (card == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This card doesn't exist!");
                }

                var boardId = card.CardList.BoardId;

                if (!await _userBoardService.IsUserMemberOfBoard(unassigningUserUsername, boardId))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("You don't have access to this board.");
                }

                var cardAssignee = await _cardAssigneeService.GetById(card.Id, user.Id, boardId);
                if (cardAssignee == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("This user is not assigned to this card!");
                }

                await _unitOfWork.CardAssignees.Delete(
                    cardAssignee.CardId,
                    cardAssignee.UserId,
                    cardAssignee.BoardId
                );

                await _unitOfWork.SaveChangesAsync();

                if (user.Id != unassigningUser.Id)
                {
                    await _notificationService.Create(
                        recipientUserId: user.Id,
                        creatingUserId: unassigningUser.Id,
                        type: NotificationType.UNASSIGNED_FROM_CARD,
                        message: $"{unassigningUser.Username} unassigned you from card '{card.Name}'.",
                        boardId: boardId,
                        cardId: card.Id
                    );
                }

                await _boardActivityService.Create(
                    boardId: boardId,
                    creatingUserId: unassigningUser.Id,
                    type: ActivityType.USER_UNASSIGNED_FROM_CARD,
                    message: $"{unassigningUser.Username} unassigned {user.Username} from card '{card.Name}'.",
                    cardId: card.Id
                );

                await _unitOfWork.CommitAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"An error occurred during unassignment: {ex.Message}");
            }
        }

        public async Task<Result<List<UserDTO>>> GetUsersAssignedToCard(int cardId)
        {
            var card = await _cardRepository.GetById(cardId);
            if (card == null)
            {
                return Result.Fail("This card doesn't exist!");
            }

            if (card.Assignees == null || !card.Assignees.Any())
            {
                return Result.Ok(new List<UserDTO>());
            }

            var users = card.Assignees
                .Select(a => a.UserBoard.User)
                .ToList();

            return Result.Ok(_mapper.Map<List<UserDTO>>(users));
        }

        public async Task<Result<CardDTO>> Update(CardDTO cardDTO, string username)
        {
            Card existingCard = await _cardRepository.GetById(cardDTO.Id);
            if (existingCard == null)
            {
                return Result.Fail("Card with the given ID was not found.");
            }

            if (!await _userBoardService.IsUserMemberOfBoard(username, existingCard.CardList.BoardId))
            {
                return Result.Fail("You don't have access to this board.");
            }

            try
            {
                existingCard.Name = cardDTO.Name;
                existingCard.Description = cardDTO.Description;

                var updatedCard = await _cardRepository.Update(existingCard);

                if (updatedCard == null)
                {
                    return Result.Fail("Failed to update the Card.");
                }

                var user = await _userService.GetUserByUsername(username);
                if (user != null)
                {
                    await _boardActivityService.Create(
                        boardId: updatedCard.CardList.BoardId,
                        creatingUserId: user.Id,
                        type: ActivityType.CARD_UPDATED,
                        message: $"{user.Username} updated card '{updatedCard.Name}'.",
                        cardId: updatedCard.Id
                    );
                }

                return Result.Ok(_mapper.Map<Card, CardDTO>(updatedCard));
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during update: {ex.Message}");
            }
        }

        public async Task<Result> Delete(int id, string username)
        {
            var card = await _cardRepository.GetById(id);

            if (card == null)
            {
                return Result.Fail("Card with the given ID was not found.");
            }

            if (!await _userBoardService.IsUserMemberOfBoard(username, card.CardList.BoardId))
            {
                return Result.Fail("You don't have access to this board.");
            }

            try
            {
                var boardId = card.CardList.BoardId;
                var cardName = card.Name;

                await _cardRepository.Delete(id);

                var user = await _userService.GetUserByUsername(username);
                if (user != null)
                {
                    await _boardActivityService.Create(
                        boardId: boardId,
                        creatingUserId: user.Id,
                        type: ActivityType.CARD_DELETED,
                        message: $"{user.Username} deleted card '{cardName}'."
                    );
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during deletion: {ex.Message}");
            }
        }

        public async Task<Result> TogglePin(int cardId, string username)
        {
            var card = await _cardRepository.GetById(cardId);
            if (card == null)
            {
                return Result.Fail("Card with the given ID was not found.");
            }

            if (!await _userBoardService.IsUserOwnerOfBoard(username, card.CardList.BoardId))
            {
                return Result.Fail("Only the board owner can pin or unpin cards.");
            }

            card.IsPinned = !card.IsPinned;
            var updatedCard = await _cardRepository.Update(card);

            var listCards = await _cardRepository.GetByListId(updatedCard.CardListId);
            for (int i = 0; i < listCards.Count; i++)
            {
                listCards[i].Index = i + 1;
            }
            await _cardRepository.UpdateRangeAsync(listCards);

            var user = await _userService.GetUserByUsername(username);
            if (user != null)
            {
                await _boardActivityService.Create(
                    boardId: card.CardList.BoardId,
                    creatingUserId: user.Id,
                    type: updatedCard.IsPinned ? ActivityType.CARD_PINNED : ActivityType.CARD_UNPINNED,
                    message: $"{user.Username} {(updatedCard.IsPinned ? "pinned" : "unpinned")} card '{updatedCard.Name}'.",
                    cardId: updatedCard.Id
                );
            }

            return Result.Ok();
        }
    }
}