using FakeTrello.Data;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;

namespace FakeTrello.Repository
{
    public class CardRepository : ICardRepository
    {
        private readonly MyDbContext _context;
        public CardRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Card> Create(Card card)
        {
            await _context.Cards.AddAsync(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<int?> GetMaxIndexForCardAsync(int cardListId)
        {
            return await _context.Cards
                .Where(c => c.CardListId == cardListId)
                .MaxAsync(c => (int?)c.Index);
        }

        public async Task<List<Card>> GetAll()
        {
            return await _context.Cards.ToListAsync();
        }

        public async Task<Card?> GetById(int id)
        {
            return await _context.Cards
                .Include(c => c.CardList)
                .Include(c => c.Assignees)
                    .ThenInclude(a => a.UserBoard)
                        .ThenInclude(ub => ub.User)
                .Include(c => c.Images.Where(i => i.Status != EntityStatus.DELETED))
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Card>> GetByListId(int listId)
        {
            return await _context.Cards
                .Include(c => c.Assignees)
                    .ThenInclude(a => a.UserBoard)
                        .ThenInclude(ub => ub.User)
                .Include(c => c.Images.Where(i => i.Status != EntityStatus.DELETED))
                .Include(c => c.CreatedByUser)
                .Where(c => c.CardListId == listId && c.Status != EntityStatus.DELETED)
                .OrderByDescending(c => c.IsPinned)
                .ThenBy(c => c.Index)
                .ToListAsync();
        }

        public async Task<Card> Update(Card card)
        {
            _context.Cards.Update(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task UpdateRangeAsync(List<Card> cards)
        {
            _context.Cards.UpdateRange(cards);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            Card? card = await GetById(id);
            if (card == null)
            {
                throw new KeyNotFoundException();
            }

            card.Status = EntityStatus.DELETED;
            _context.Cards.Update(card);
            await _context.SaveChangesAsync();

            List<Card> cards = await GetByListId(card.CardListId);
            for (int i = 0; i < cards.Count; i++)
                cards[i].Index = i + 1;
            _context.Cards.UpdateRange(cards);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRange(IEnumerable<int> ids)
        {
            await _context.Cards
                .Where(c => ids.Contains(c.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, EntityStatus.DELETED));
        }

        public async Task MoveToAnotherList(int id, int targetListId)
        {
            Card? card = await GetById(id);
            if (card == null)
            {
                throw new KeyNotFoundException();
            }
            int oldList = card.CardListId;
            card.CardListId = targetListId;
            _context.Cards.Update(card);
            await _context.SaveChangesAsync();

            List<Card> cards = await GetByListId(oldList);
            for (int i = 0; i < cards.Count; i++)
                cards[i].Index = i + 1;
            _context.Cards.UpdateRange(cards);
            await _context.SaveChangesAsync();
        }

    }
}