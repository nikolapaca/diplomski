using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace FakeTrello.Service
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _cardRepository;
        private readonly ICardListService _cardListService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public CardService(ICardRepository cardRepository, IMapper mapper, ICardListService cardListService, IUserService userService)
        {
            _cardRepository = cardRepository;
            _mapper = mapper;
            _cardListService = cardListService;
            _userService = userService;
        }

        public async Task<Result<CardDTO>> Create(int cardListId, CardDTO cardDTO)
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

            if (oldIndex == newIndex) return Result.Fail("");

            if (oldIndex < newIndex)
            {
                foreach (var c in cards.Where(c => c.Index > oldIndex && c.Index <= newIndex))
                    c.Index--;
            }
            else
            {
                foreach (var c in cards.Where(c => c.Index >= newIndex && c.Index < oldIndex))
                    c.Index++;
            }

            card.Index = newIndex;

            await _cardRepository.UpdateRangeAsync(cards.Append(card).ToList());
            return Result.Ok();
        }

        public async Task<Result> ReorderCardOutsideList(int cardId, int targetListId, int targetIndex)
        {
            var card = await _cardRepository.GetById(cardId);
            if (card == null)
                throw new Exception("Card not found");

            await _cardRepository.MoveToAnotherList(card.Id, targetListId);

            var cards = await _cardRepository.GetByListId(targetListId);
            targetIndex = Math.Clamp(targetIndex, 1, cards.Count + 1);
            card.Index = targetIndex;

            for (int i = 0; i < cards.Count; i++)
                cards[i].Index = i + 1;
            await _cardRepository.UpdateRangeAsync(cards);

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
                if(card.UserId != null)
                    cardDTO.AssignedUserUsername = card.User.Username;
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
                card.UserId = user.Id;
                var updatedCard = await _cardRepository.Update(card);
                if (updatedCard == null)
                {
                    return Result.Fail("Failed to update the Card.");
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during update: {ex.Message}");
            }
        }

        public async Task<Result> UnassignCardToUser(CardDTO cardDto)
        {
            var card = await _cardRepository.GetById(cardDto.Id);
            if (card == null)
            {
                return Result.Fail("This card doesn't exist!");
            }
            try
            {
                card.UserId = null;
                var updatedCard = await _cardRepository.Update(card);
                if (updatedCard == null)
                {
                    return Result.Fail("Failed to update the Card.");
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during update: {ex.Message}");
            }
        }

        public async Task<Result<UserDTO>> GetUserAssignedToCard(int cardId)
        {
            var card = await _cardRepository.GetById(cardId);
            if(card == null)
            {
                return Result.Fail("This card doesn't exist!");
            }
            if (card.UserId.HasValue) 
            {
                var user = await _userService.GetById(card.UserId);
                if(user == null)
                {
                    return Result.Fail("This user doesn't exist!");
                }
                return user;
            }
            else
            {
                return Result.Ok();
            }
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