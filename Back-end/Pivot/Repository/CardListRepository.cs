using Pivot.Data;
using Pivot.Model;
using Pivot.Model.Enum;
using Pivot.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace Pivot.Repository
{
    public class CardListRepository : ICardListRepository
    {
        private readonly MyDbContext _context;
        public CardListRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<CardList> Create(CardList cardList)
        {
            await _context.CardLists.AddAsync(cardList);
            await _context.SaveChangesAsync();
            return cardList;
        }

        public async Task<int?> GetMaxIndexForCardListAsync(int boardId)
        {
            return await _context.CardLists
                .Where(c => c.BoardId == boardId)
                .MaxAsync(c => (int?)c.Index);
        }

        public async Task<List<CardList>> GetAll()
        {
            return await _context.CardLists.ToListAsync();
        }

        public async Task<CardList?> GetById(int id)
        {
            return await _context.CardLists.Include(cl => cl.Cards)
                .ThenInclude(c => c.Assignees)
                .ThenInclude(a => a.UserBoard)
                .ThenInclude(ub => ub.User)
                .FirstOrDefaultAsync(cl => cl.Id == id);
        }

        public async Task<List<CardList>> GetByBoardId(int boardId)
        {
            return await _context.CardLists.Include(cl => cl.Cards).ThenInclude(c => c.Assignees).Where(b => b.BoardId == boardId)
                .OrderByDescending(cl => cl.IsPinned)
                .ThenBy(cl => cl.Index)
                .ToListAsync();
        }

        public async Task<List<CardList>> GetByBoardOwnerAndBoardName(string boardName, string boardOwnerUsername)
        {
            var cardLists = await _context.CardLists
                                .Include(cl => cl.Cards.Where(c => c.Status != EntityStatus.DELETED).OrderByDescending(c => c.IsPinned).ThenBy(c => c.Index))
                                .ThenInclude(c => c.Assignees)
                                .Include(cl => cl.Cards)
                                .ThenInclude(c => c.Images.Where(i => i.Status != EntityStatus.DELETED))
                                .Include(cl => cl.Cards)
                                .ThenInclude(c => c.CreatedByUser)
                                .Include(cl => cl.Board)
                                .ThenInclude(b => b.UserBoards)
                                .ThenInclude(ub => ub.User)
                                .Where(cl => cl.Board.Name == boardName &&
                                             cl.Board.UserBoards.Any(ub => ub.User.Username == boardOwnerUsername) &&
                                             cl.Status != EntityStatus.DELETED)
                                .OrderByDescending(cl => cl.IsPinned)
                                .ThenBy(cl => cl.Index)
                            .ToListAsync();

            return cardLists;
        }

        public async Task<CardList> Update(CardList cardList)
        {
            _context.CardLists.Update(cardList);
            await _context.SaveChangesAsync();
            return cardList;
        }

        public async Task Delete(int id)
        {
            CardList? cardList = await GetById(id);
            if (cardList == null)
            {
                throw new KeyNotFoundException();
            }

            cardList.Status = EntityStatus.DELETED;
            _context.CardLists.Update(cardList);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRange(IEnumerable<int> ids)
        {
            await _context.CardLists
                .Where(l => ids.Contains(l.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, EntityStatus.DELETED));
        }

        public async Task UpdateRangeAsync(List<CardList> cardLists)
        {
            _context.CardLists.UpdateRange(cardLists);
            await _context.SaveChangesAsync();
        }
    }
}