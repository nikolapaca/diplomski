using Pivot.DTO;
using FluentResults;

namespace Pivot.Service.Contract
{
    public interface ICardListService
    {
        Task<Result<List<CardListDTO>>> GetAll();
        Task<Result<List<CardListDTO>>> GetByBoardNameAndBoardOwner(string boardName, string boardOwnerUsername);
        Task<Result<CardListDTO>> Create(CardListDTO cardListDTO, string username);
        Task<Result<CardListDTO>> GetById(int id);
        Task<int?> GetBoardIdByListId(int listId);
        Task<Result<(string Name, int BoardId)>> GetNameAndBoardId(int listId);
        Task<Result<CardListDTO>> Update(CardListDTO cardListDTO, string username);
        Task<Result> Delete(int id, string username);
        Task<Result> MoveList(CardListDTO cardList, int targetIndex, string username);
        Task<Result> TogglePin(int listId, string username);
    }
}