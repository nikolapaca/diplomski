using FakeTrello.Model;

namespace FakeTrello.Repository.Contract
{
    public interface ICardAssigneeRepository
    {
        Task<CardAssignee> GetById(int cardId, int userId, int boardId);
        Task<List<CardAssignee>> GetAll();
        Task<List<CardAssignee>> GetByCardId(int cardId);
        Task<CardAssignee> CreateAsync(CardAssignee cardAsignee);
        Task<CardAssignee> UpdateAsync(CardAssignee cardAsignee);
        Task Delete(int cardId, int userId, int boardId);
    }
}
