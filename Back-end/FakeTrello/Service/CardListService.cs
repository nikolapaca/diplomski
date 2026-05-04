using AutoMapper;
using FakeTrello.Data.Contract;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;

namespace FakeTrello.Service
{
    public class CardListService : ICardListService
    {
        private readonly ICardListRepository _cardListRepository;
        private readonly IBoardService _boardService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CardListService(ICardListRepository cardListRepository, IMapper mapper, IBoardService boardService, IUnitOfWork unitOfWork)
        {
            _cardListRepository = cardListRepository;
            _mapper = mapper;
            _boardService = boardService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CardListDTO>> Create(CardListDTO cardListDTO)
        {
            CardList cardList = _mapper.Map<CardListDTO, CardList>(cardListDTO);
            Board? board = await _boardService.GetByNameAndOwnerUsername(cardListDTO.BoardName, cardListDTO.BoardUsername);
            if(board == null)
            {
                return Result.Fail("Board doesn't exist!");
            }
            cardList.BoardId = board.Id;
            var index = await _cardListRepository.GetMaxIndexForCardListAsync(board.Id);
            cardList.Index = index is null ? 1 : (int)index + 1;
            await _cardListRepository.Create(cardList);
            return Result.Ok(_mapper.Map<CardList, CardListDTO>(cardList));
        }

        public async Task<Result<List<CardListDTO>>> GetAll()
        {
            return _mapper.Map<List<CardList>, List<CardListDTO>>(await _cardListRepository.GetAll());
        }

        public async Task<Result<CardListDTO>> GetById(int id)
        {
            return _mapper.Map<CardList?, CardListDTO>(await _cardListRepository.GetById(id));
        }

        public async Task<Result<List<CardListDTO>>> GetByBoardNameAndBoardOwner(string boardName, string boardOwnerUsername)
        {
            return _mapper.Map<List<CardList>, List<CardListDTO>>(await _cardListRepository.GetByBoardOwnerAndBoardName(boardName, boardOwnerUsername));
        }

        public async Task<Result<CardListDTO>> Update(CardListDTO cardListDTO)
        {
            CardList cardList = await _cardListRepository.GetById(cardListDTO.Id);
            if (cardList == null)
            {
                return Result.Fail("CardList with the given ID was not found.");
            }
            try
            {
                cardList.Name = cardListDTO.Name;
                var updatedCardList = await _cardListRepository.Update(cardList);

                if (updatedCardList == null)
                {
                    return Result.Fail("Failed to update the CardList.");
                }

                return Result.Ok(_mapper.Map<CardList, CardListDTO>(updatedCardList));
            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred during update: {ex.Message}");
            }
        }

        public async Task<Result> Delete(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var getByIdResult = await GetById(id);

                if (!getByIdResult.IsSuccess)
                {
                    return Result.Fail(getByIdResult.Errors.First().Message);
                }
                
                foreach(var card in getByIdResult.Value.Cards)
                {
                    await _unitOfWork.Cards.Delete(card.Id);
                }
                await _unitOfWork.CardLists.Delete(id);

                await _unitOfWork.CommitAsync();
                return Result.Ok();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("Failed to delete this card.");
            }
        }

        public async Task<Result> MoveList(CardListDTO cardList, int targetIndex)
        {
            var list = await _cardListRepository.GetById(cardList.Id);
            if (list == null)
                throw new Exception("List not found");

            var lists = await _cardListRepository.GetByBoardId(list.BoardId);
            int oldIndex = list.Index;

            if (oldIndex == targetIndex) return Result.Fail("");

            if (oldIndex < targetIndex)
            {
                foreach (var c in lists.Where(c => c.Index > oldIndex && c.Index <= targetIndex))
                    c.Index--;
            }
            else
            {
                foreach (var c in lists.Where(c => c.Index >= targetIndex && c.Index < oldIndex))
                    c.Index++;
            }

            list.Index = targetIndex;

            await _cardListRepository.UpdateRangeAsync(lists.Append(list).ToList());
            return Result.Ok();
        }
    }
}
