using Pivot.Data;
using Pivot.Model;
using Pivot.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace Pivot.Repository
{
    public class CardAssigneeRepository : ICardAssigneeRepository
    {
        private readonly MyDbContext _context;

        public CardAssigneeRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<CardAssignee>> GetAll()
        {
            return await _context.CardAssignees.ToListAsync();
        }

        public async Task<CardAssignee?> GetById(int cardId, int userId, int boardId)
        {
            return await _context.CardAssignees
                        .Include(ca => ca.UserBoard)
                        .FirstOrDefaultAsync(ca =>
                            ca.CardId == cardId &&
                            ca.UserId == userId &&
                            ca.BoardId == boardId);
        }

        public async Task<List<CardAssignee>> GetByCardId(int cardId)
        {
            return await _context.CardAssignees.Where(ca => ca.CardId == cardId).ToListAsync();
        }

        public async Task<CardAssignee> CreateAsync(CardAssignee ca)
        {
            await _context.CardAssignees.AddAsync(ca);
            await _context.SaveChangesAsync();
            return ca;
        }

        public async Task<CardAssignee> UpdateAsync(CardAssignee ca)
        {
            _context.CardAssignees.Update(ca);
            await _context.SaveChangesAsync();
            return ca;
        }

        public async Task Delete(int cardId, int userId, int boardId)
        {
            CardAssignee ca = await GetById(cardId, userId, boardId);
            if (ca == null) throw new KeyNotFoundException();
            _context.CardAssignees.Remove(ca);
            await _context.SaveChangesAsync();
        }
    }
}
