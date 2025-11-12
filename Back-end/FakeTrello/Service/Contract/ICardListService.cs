using FakeTrello.DTO;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface ICardListService
    {
        Task<Result<List<CardListDTO>>> GetAll();
        Task<Result<List<CardListDTO>>> GetByBoardNameAndBoardOwner(string boardName, string boardOwnerUsername);
        Task<Result<CardListDTO>> Create(CardListDTO cardListDTO);
        Task<Result<CardListDTO>> GetById(int id);
        Task<Result<CardListDTO>> Update(CardListDTO cardListDTO);
        Task<Result> Delete(int id);
        Task<Result> MoveList(CardListDTO cardList, int targetIndex);
    }
}
