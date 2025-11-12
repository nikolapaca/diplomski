using FakeTrello.DTO;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface ICardService
    {
        Task<Result<List<CardDTO>>> GetAll();
        Task<Result<CardDTO>> Create(int cardListId, CardDTO cardDTO);
        Task<Result<CardDTO>> GetById(int id);
        Task<Result<CardDTO>> Update(CardDTO cardLDTO);
        Task<Result<List<CardDTO>>> GetByListId(int listId);
        Task<Result> Delete(int id);
        Task<Result> AssignCardToUser(CardDTO cardDto, string username);
        Task<Result<UserDTO>> GetUserAssignedToCard(int cardId);
        Task<Result> UnassignCardToUser(CardDTO cardDto);
        Task<Result> ReorderCardInsideList(int cardId, int newIndex);
        Task<Result> ReorderCardOutsideList(int cardId, int targetListId, int targetIndex);
    }
}
