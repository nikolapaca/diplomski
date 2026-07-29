using FakeTrello.DTO;
using FakeTrello.Model;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface IBoardActivityService
    {
        Task<Result<BoardActivityDTO>> Create(int boardId, int creatingUserId, ActivityType type, string message, int? cardId = null);

        Task<Result<List<BoardActivityDTO>>> GetByBoard(string boardName, string ownerUsername);

        Task<Result<List<BoardActivityDTO>>> GetByCard(int cardId, string username);
    }
}