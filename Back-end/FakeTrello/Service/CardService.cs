using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace FakeTrello.Service
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _cardRepository;
        private readonly ICardListService _cardListService;
        private readonly IUserService _userService;
        private readonly ICardAssigneeService _cardAssigneeService;
        private readonly IMapper _mapper;

        public CardService(ICardRepository cardRepository, IMapper mapper, ICardListService cardListService, IUserService userService, ICardAssigneeService cardAssigneeService)
        {
            _cardRepository = cardRepository;
            _mapper = mapper;
            _cardListService = cardListService;
            _userService = userService;
            _cardAssigneeService = cardAssigneeService;
        }

        public async Task<Result<CardDTO>> Create(int cardListId, CardDTO cardDTO, int userId)
        {
            var listResult = await _cardListService.GetById(cardListId);
            if (listResult.IsFailed)
            {
                return listResult.ToResult<CardDTO>();
            }
            CardListDTO list = listResult.Value;

            Card card = _mapper.Map<CardDTO, Card>(cardDTO);
            card.CardListId = list.Id;
            card.Status = EntityStatus.ACTIVE;
            var maxIndex = await _cardRepository.GetMaxIndexForCardAsync(list.Id);
            card.Index = (maxIndex ?? 0) + 1;
            card.CreatedByUserId = userId;

            await _cardRepository.Create(card);

            return Result.Ok(_mapper.Map<Card, CardDTO>(card));
        }

        public async Task<Result> ReorderCardInsideList(int cardId, int newIndex)
        {
            var card = await _cardRepository.GetById(cardId);
            if (card == null)
                throw new Exception("Card not found");

            var cards = await _cardRepository.GetByListId(card.CardListId);

            int oldIndex = card.Index;
            newIndex = Math.Clamp(newIndex, 1, cards.Count);

            if (oldIndex == newIndex)
                return Result.Ok();

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

            await _cardRepository.UpdateRangeAsync(cards);
            return Result.Ok();
        }

        public async Task<Result> ReorderCardOutsideList(int cardId, int targetListId, int targetIndex)
        {
            var card = await _cardRepository.GetById(cardId);
            if (card == null)
                throw new Exception("Card not found");

            int oldListId = card.CardListId;

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

            await _cardRepository.UpdateRangeAsync(cardsToUpdate);

            return Result.Ok();
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

        public async Task<Result> AssignCardToUser(CardDTO cardDto, string username)
        {
            var user = await _userService.GetUserByUsername(username);
            if (user == null)
            {
                return Result.Fail("This user doesn't exist!");
            }

            var card = await _cardRepository.GetById(cardDto.Id);
            if (card == null)
            {
                return Result.Fail("This card doesn't exist!");
            }

            try
            {
                var boardId = card.CardList.BoardId;

                CardAssignee ca = new CardAssignee(card.Id, user.Id, boardId);
                await _cardAssigneeService.Create(ca);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during assignment: {ex.Message}");
            }
        }

        public async Task<Result> UnassignCardToUser(CardDTO cardDto, string username)
        {
            var user = await _userService.GetUserByUsername(username);
            if (user == null)
            {
                return Result.Fail("This user doesn't exist!");
            }

            var card = await _cardRepository.GetById(cardDto.Id);
            if (card == null)
            {
                return Result.Fail("This card doesn't exist!");
            }

            try
            {
                var boardId = card.CardList.BoardId;

                var cardAssignee = await _cardAssigneeService.GetById(card.Id, user.Id, boardId);
                if (cardAssignee == null)
                {
                    return Result.Fail("This user is not assigned to this card!");
                }

                await _cardAssigneeService.Delete(cardAssignee.CardId, cardAssignee.UserId, cardAssignee.BoardId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
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

        public async Task<Result<CardDTO>> Update(CardDTO cardDTO)
        {
            Card existingCard = await _cardRepository.GetById(cardDTO.Id);
            if (existingCard == null)
            {
                return Result.Fail("Card with the given ID was not found.");
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

                return Result.Ok(_mapper.Map<Card, CardDTO>(updatedCard));
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during update: {ex.Message}");
            }
        }

        public async Task<Result> Delete(int id)
        {
            var getByIdResult = await GetById(id);

            if (!getByIdResult.IsSuccess)
            {
                return Result.Fail(getByIdResult.Errors.First().Message);
            }

            try
            {
                await _cardRepository.Delete(id);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during deletion: {ex.Message}");
            }
        }
    }
}