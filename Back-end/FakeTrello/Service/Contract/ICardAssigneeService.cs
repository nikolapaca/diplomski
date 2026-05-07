using FakeTrello.Model;

namespace FakeTrello.Service.Contract
{
    public interface ICardAssigneeService
    {
        Task<List<CardAssignee>> GetAll();
        Task<List<CardAssignee>> GetByCardId(int cardId);
        Task<CardAssignee> GetById(int cardId, int userId, int boardId);
        Task<CardAssignee> Create(CardAssignee cardAssignee);
        Task<CardAssignee> Update(CardAssignee cardAssignee);
        Task Delete(int cardId, int userId, int boardId);
    }
}
