using Pivot.Model;

namespace Pivot.Repository.Contract
{
    public interface ICardRepository
    {
        Task<Card> Create(Card cardList);
        Task<List<Card>> GetAll();
        Task<Card?> GetById(int id);
        Task<List<Card>> GetByListId(int listId);
        Task<Card> Update(Card cardList);
        Task UpdateRangeAsync(List<Card> cards);
        Task Delete(int id);
        Task DeleteRange(IEnumerable<int> ids);
        Task MoveToAnotherList(int id, int targetListId);
        Task<int?> GetMaxIndexForCardAsync(int cardListId);
    }
}