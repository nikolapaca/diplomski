using FakeTrello.Data;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Repository
{
    public class BoardRepository : IBoardRepository
    {
        private readonly MyDbContext _context;

        public BoardRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<Board>> GetAll()
        {
            var list = await _context.Boards.ToListAsync();
            return list;
        }

        public async Task<Board?> GetById(int id)
        {
            return await _context.Boards.FirstOrDefaultAsync(b => b.Id == id && b.Status == BoardStatus.ACTIVE);
        }

        public async Task<List<Board>> GetByName(string name)
        {
            return await _context.Boards.Include(b => b.UserBoards).Where(b => b.Name.Equals(name) && b.Status == BoardStatus.ACTIVE).ToListAsync();
        }

        public async Task<List<Board>> GetAllOwnedByUser(User user)
        {
            return await _context.Boards.Where(b => b.UserBoards.Any(ub => ub.UserId == user.Id && ub.UserRole == UserRole.OWNER) && b.Status == BoardStatus.ACTIVE).ToListAsync();
        }

        public async Task<List<Board>> GetAllArchivedOwnedByUser(User user)
        {
            return await _context.Boards
                .Where(b => b.UserBoards.Any(ub => ub.UserId == user.Id && ub.UserRole == UserRole.OWNER) && b.Status == BoardStatus.ARCHIVED)
                .OrderByDescending(b => b.Id)
                .ToListAsync();
        }

        public async Task<List<Board>> GetAllByUserIdOrdered(User user)
        {
            return await _context.Boards.Include(b => b.UserBoards).Where(b => b.UserBoards.Any(ub => ub.UserId == user.Id) && b.Status == BoardStatus.ACTIVE)
                    .Select(b => new
                    {
                        Board = b,
                        UserBoard = b.UserBoards.First(ub => ub.UserId == user.Id)
                    })
                    .OrderByDescending(item => item.UserBoard.IsFavorite)
                    .ThenBy(item => item.UserBoard.UserRole)
                    .Select(item => item.Board).ToListAsync();
        }

        public async Task<List<Board>> GetBySearchFilter(int userId, string searchTerm)
        {
            string[] searchTerms = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return await _context.Boards.Include(b => b.UserBoards)
                .Where(b => b.UserBoards.Any(ub => ub.UserId == userId) && b.Status == BoardStatus.ACTIVE)
                .Where(b => searchTerms.All(term => b.Name.ToLower().Contains(term) || b.Description.ToLower().Contains(term)
                || b.UserBoards.Any(ub => ub.UserRole == UserRole.OWNER && ub.User.Username.ToLower().Contains(term))))
                .OrderByDescending(b => b.UserBoards.Any(ub => ub.UserId == userId && ub.UserRole == UserRole.OWNER))
                .ThenByDescending(b => (
                (b.Name.ToLower().Equals(searchTerm) ? 1000 : 0) +
                (b.Name.ToLower().StartsWith(searchTerm) ? 100 : 0) +
                (b.Name.ToLower().Contains(searchTerm) ? 10 : 0) +
                (b.Description.ToLower().Contains(searchTerm) ? 1 : 0))).ToListAsync();
        }

        public async Task<Board?> GetByNameAndOwnerUsername(string name, string username)
        {
            return await _context.Boards.Include(ub => ub.UserBoards).Include(ub => ub.Lists).ThenInclude(c => c.Cards).FirstOrDefaultAsync(board =>
                board.Name.Equals(name) && board.Status == BoardStatus.ACTIVE &&
                board.UserBoards.Any(ub => ub.User.Username == username && ub.UserRole == UserRole.OWNER));
        }

        public async Task<Board?> GetArchivedByNameAndOwnerUsername(string name, string username)
        {
            return await _context.Boards.Include(ub => ub.UserBoards).FirstOrDefaultAsync(board =>
                board.Name.Equals(name) && board.Status == BoardStatus.ARCHIVED &&
                board.UserBoards.Any(ub => ub.User.Username == username && ub.UserRole == UserRole.OWNER));
        }

        public async Task<Board?> Create(Board board)
        {
            await _context.Boards.AddAsync(board);
            await _context.SaveChangesAsync();
            return board;
        }

        public async Task<Board> Update(Board board)
        {
            _context.Boards.Update(board);
            await _context.SaveChangesAsync();
            return board;
        }

        public async Task Delete(int id)
        {
            Board board = await GetById(id);
            if (board == null)
            {
                throw new KeyNotFoundException();
            }

            board.Status = BoardStatus.DELETED;
            _context.Boards.Update(board);
            await _context.SaveChangesAsync();
        }
    }
}