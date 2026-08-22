using FakeTrello.Model;

namespace FakeTrello.Repository.Contract
{
    public interface IBoardRepository
    {
        Task<List<Board>> GetAll();
        Task<Board> GetById(int id);
        Task<List<Board>?> GetByName(string name);
        Task<List<Board>> GetAllOwnedByUser(User user);
        Task<List<Board>> GetAllArchivedOwnedByUser(User user);
        Task<List<Board>> GetAllByUserIdOrdered(User user);
        Task<List<Board>> GetBySearchFilter(int userId, string searchTerm);
        Task<Board?> GetByNameAndOwnerUsername(string name, string username);
        Task<Board?> GetArchivedByNameAndOwnerUsername(string name, string username);
        Task<Board> Create(Board board);
        Task<Board> Update(Board board);
        Task Delete(int id);
    }
}