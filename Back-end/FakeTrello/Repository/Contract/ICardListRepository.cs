using FakeTrello.Model;
using System.Threading.Tasks;

namespace FakeTrello.Repository.Contract
{
    public interface ICardListRepository
    {
        Task<CardList> Create(CardList cardList);
        Task<List<CardList>> GetAll();
        Task<CardList?> GetById(int id);
        Task<List<CardList>> GetByBoardId(int boardId);
        Task<int?> GetMaxIndexForCardListAsync(int boardId);
        Task<List<CardList>> GetByBoardOwnerAndBoardName(string boardName, string boardOwnerUsername);
        Task<CardList> Update(CardList cardList);
        Task Delete(int id);
        Task UpdateRangeAsync(List<CardList> cardLists);
    }
}
