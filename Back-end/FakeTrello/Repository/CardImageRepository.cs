using FakeTrello.Data;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Repository
{
    public class CardImageRepository : ICardImageRepository
    {
        private readonly MyDbContext _context;

        public CardImageRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<CardImage> Create(CardImage image)
        {
            await _context.CardImages.AddAsync(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<CardImage?> GetById(int id)
        {
            return await _context.CardImages
                .Include(i => i.UploadedByUser)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<CardImage>> GetByCardId(int cardId)
        {
            return await _context.CardImages
                .Include(i => i.UploadedByUser)
                .Where(i => i.CardId == cardId)
                .OrderBy(i => i.UploadedAt)
                .ToListAsync();
        }

        public async Task Delete(int id)
        {
            var image = await _context.CardImages.FindAsync(id);
            if (image != null)
            {
                _context.CardImages.Remove(image);
                await _context.SaveChangesAsync();
            }
        }
    }
}
