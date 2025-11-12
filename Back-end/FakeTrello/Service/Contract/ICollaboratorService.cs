using FakeTrello.DTO;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface ICollaboratorService
    {
        Task<Result<List<UserDTO>>> GetUsersNotOnBoard(string searchTerm, string boardName, string boardOwnerUsername);
        Task<Result<List<UserDTO>>> GetUsersOnBoard(string searchTerm, string boardName, string boardOwnerUsername);
        Task<Result<List<UserDTO>>> GetAssignableUsersOnBoard(string searchTerm, string boardName, string boardOwnerUsername, int cardId);
    }
}
