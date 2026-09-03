using Pivot.Data;
using Pivot.Model;
using Pivot.Model.Enum;
using Pivot.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace Pivot.Repository
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
                .FirstOrDefaultAsync(i => i.Id == id && i.Status != EntityStatus.DELETED);
        }

        public async Task<List<CardImage>> GetByCardId(int cardId)
        {
            return await _context.CardImages
                .Include(i => i.UploadedByUser)
                .Where(i => i.CardId == cardId && i.Status != EntityStatus.DELETED)
                .OrderBy(i => i.UploadedAt)
                .ToListAsync();
        }

        public async Task Delete(int id)
        {
            var image = await _context.CardImages.FindAsync(id);
            if (image != null)
            {
                image.Status = EntityStatus.DELETED;
                _context.CardImages.Update(image);
                await _context.SaveChangesAsync();
            }
        }
    }
}