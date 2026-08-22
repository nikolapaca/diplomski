using FakeTrello.DTO;
using FluentResults;

namespace FakeTrello.Service.Contract
{
    public interface ICommentService
    {
        Task<Result<CommentDTO>> Create(int cardId, string text, string username);
        Task<Result<List<CommentDTO>>> GetByCard(int cardId, string username);
        Task<Result> Delete(int commentId, string username);
    }
}
