using Pivot.Model;

namespace Pivot.Repository.Contract
{
    public interface ICommentRepository
    {
        Task<Comment> Create(Comment comment);
        Task<Comment?> GetById(int id);
        Task<List<Comment>> GetByCardId(int cardId);
        Task Delete(int id);
    }
}
