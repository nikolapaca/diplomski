using FakeTrello.Data;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Repository
{
    public class UserBoardRepository : IUserBoardRepository
    {
        private readonly MyDbContext _context;
        
        public UserBoardRepository(MyDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task<List<UserBoard>> GetAll()
        {
            var list = await _context.UserBoards.ToListAsync();
            return list;
        }

        public async Task<UserBoard?> GetByUserAndBoardId(int userId, int boardId)
        {
            return await _context.UserBoards.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BoardId == boardId);
        }

        public async Task<UserBoard> GetByOwnerRoleAndBoardId(int boardId)
        {
            return await _context.UserBoards.FirstOrDefaultAsync(b => b.BoardId == boardId && b.UserRole == UserRole.OWNER);
        }

        public async Task<string?> GetUsernameOfBoardOwner(int boardId)
        {
            return await _context.UserBoards.Where(ub => ub.BoardId == boardId && ub.UserRole == UserRole.OWNER).Include(ub => ub.User)
                .Select(ub => ub.User.Username)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserBoard>> GetAllByUserId(int userId)
        {
            return await _context.UserBoards.Where(ub => ub.UserId == userId).OrderBy(ub => ub.UserRole).ToListAsync();
        }

        public async Task<UserBoard> CreateAsync(UserBoard userBoard)
        {
            await _context.UserBoards.AddAsync(userBoard);
            await _context.SaveChangesAsync();
            return userBoard;
        }

        public async Task<UserBoard> UpdateAsync(UserBoard userBoard)
        {
            _context.UserBoards.Update(userBoard);
            await _context.SaveChangesAsync();
            return userBoard;
        }

        public async Task DeleteAsync(int userId, int boardId)
        {
            UserBoard? ub = await GetByUserAndBoardId(userId, boardId);
            if(ub == null)
            {
                throw new KeyNotFoundException();
            }

            _context.UserBoards.Remove(ub);
            await _context.SaveChangesAsync();
        }

        public void RemoveRange(IEnumerable<UserBoard> userBoards)
        {
            _context.UserBoards.RemoveRange(userBoards);
        }
    }
}
