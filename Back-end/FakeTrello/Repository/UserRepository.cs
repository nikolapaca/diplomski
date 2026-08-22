using FakeTrello.Data;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Repository
{
    public class UserRepository : IUserRepository
    {

        private readonly MyDbContext _context;

        public UserRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAll()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> GetById(int? id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => email.Equals(u.Email));
        }

        public async Task<User?> GetByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => username.Equals(u.Username));
        }

        public async Task<User?> Create(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> Update(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task Delete(int id)
        {
            User user = await GetById(id);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetUsersNotOnTheBoard(string searchTerm, int boardId)
        {
            if (searchTerm == null || searchTerm == "")
            {
                return await _context.Users.Where(u => !_context.UserBoards.Any(ub => ub.BoardId == boardId && ub.UserId == u.Id)).ToListAsync();
            }
            else
            {
                return await _context.Users.Where(u => !_context.UserBoards.Any(ub => ub.BoardId == boardId && ub.UserId == u.Id) && u.Username.Contains(searchTerm)).ToListAsync();
            }
        }

        public async Task<List<User>> GetUsersOnTheBoard(string searchTerm, int boardId)
        {
            if (searchTerm == null || searchTerm == "")
            {
                return await _context.Users.Where(u => _context.UserBoards.Any(ub => ub.BoardId == boardId && ub.UserId == u.Id && ub.UserRole != UserRole.OWNER)).ToListAsync();
            }
            else
            {
                return await _context.Users.Where(u => _context.UserBoards.Any(ub => ub.BoardId == boardId && ub.UserId == u.Id && ub.UserRole != UserRole.OWNER) &&
                u.Username.Contains(searchTerm)).ToListAsync();
            }
        }

        public async Task<List<User>> GetAssignableUsersOnTheBoard(string searchTerm, int boardId, int cardId)
        {
            return await _context.Users
                .Where(u =>
                    _context.UserBoards.Any(ub =>
                        ub.BoardId == boardId &&
                        ub.UserId == u.Id
                    )
                    &&
                    !_context.CardAssignees.Any(ca =>
                        ca.CardId == cardId &&
                        ca.UserId == u.Id &&
                        ca.BoardId == boardId
                    )
                    &&
                    (string.IsNullOrEmpty(searchTerm) || u.Username.Contains(searchTerm))
                )
                .ToListAsync();
        }

        public async Task<User?> GetByConfirmationToken(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
        }

        public async Task<User?> GetByResetToken(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        }
    }
}