using Pivot.Model;

namespace Pivot.Repository.Contract
{
    public interface IUserRepository
    {
        Task<List<User>> GetAll();
        Task<User> GetById(int? id);
        Task<User> GetByEmail(string email);
        Task<User> GetByUsername(string username);
        Task<User> Create(User user);
        Task<User> Update(User user);
        Task Delete(int id);
        Task<List<User>> GetUsersNotOnTheBoard(string searchTerm, int boardId);
        Task<List<User>> GetUsersOnTheBoard(string searchTerm, int boardId);
        Task<List<User>> GetAssignableUsersOnTheBoard(string searchTerm, int boardId, int cardId);
        Task<User?> GetByConfirmationToken(string confirmationToken);
        Task<User?> GetByResetToken(string resetToken);

    }
}
