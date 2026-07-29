using FakeTrello.DTO;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface ICardService
    {
        Task<Result<List<CardDTO>>> GetAll();
        Task<Result<CardDTO>> Create(int cardListId, CardDTO cardDTO, int userId);
        Task<Result<CardDTO>> GetById(int id);
        Task<Result<CardDTO>> Update(CardDTO cardLDTO, string username);
        Task<Result<List<CardDTO>>> GetByListId(int listId);
        Task<Result> Delete(int id, string username);
        Task<Result> AssignCardToUser(CardDTO cardDto, string username, string creatingUserUsername);
        Task<Result<List<UserDTO>>> GetUsersAssignedToCard(int cardId);
        Task<Result> UnassignCardToUser(CardDTO cardDto, string username, string unassigningUserUsername);
        Task<Result> ReorderCardInsideList(int cardId, int newIndex, string username);
        Task<Result> ReorderCardOutsideList(int cardId, int targetListId, int targetIndex, string username);
        Task<Result> TogglePin(int cardId, string username);
    }
}
