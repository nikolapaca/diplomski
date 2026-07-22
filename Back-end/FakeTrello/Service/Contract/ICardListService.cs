using FakeTrello.DTO;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface ICardListService
    {
        Task<Result<List<CardListDTO>>> GetAll();
        Task<Result<List<CardListDTO>>> GetByBoardNameAndBoardOwner(string boardName, string boardOwnerUsername);
        Task<Result<CardListDTO>> Create(CardListDTO cardListDTO, string username);
        Task<Result<CardListDTO>> GetById(int id);
        Task<int?> GetBoardIdByListId(int listId);
        Task<Result<CardListDTO>> Update(CardListDTO cardListDTO, string username);
        Task<Result> Delete(int id, string username);
        Task<Result> MoveList(CardListDTO cardList, int targetIndex, string username);
    }
}