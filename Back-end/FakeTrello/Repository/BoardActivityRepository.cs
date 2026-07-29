using FakeTrello.Data;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Repository
{
    public class BoardActivityRepository : IBoardActivityRepository
    {
        private readonly MyDbContext _context;

        public BoardActivityRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<BoardActivity> Create(BoardActivity activity)
        {
            await _context.BoardActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
            return activity;
        }

        public async Task<List<BoardActivity>> GetByBoardId(int boardId)
        {
            return await _context.BoardActivities
                .Include(a => a.CreatingUser)
                .Include(a => a.Card)
                .Where(a => a.BoardId == boardId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<BoardActivity>> GetByCardId(int cardId)
        {
            return await _context.BoardActivities
                .Include(a => a.CreatingUser)
                .Include(a => a.Card)
                .Where(a => a.CardId == cardId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}