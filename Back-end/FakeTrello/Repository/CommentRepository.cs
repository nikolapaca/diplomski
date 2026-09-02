using FakeTrello.Data;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Repository
{
    public class CommentRepository : ICommentRepository
    {
        private readonly MyDbContext _context;

        public CommentRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Comment> Create(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment?> GetById(int id)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Card)
                .FirstOrDefaultAsync(c => c.Id == id && c.Status != EntityStatus.DELETED);
        }

        public async Task<List<Comment>> GetByCardId(int cardId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Where(c => c.CardId == cardId && c.Status != EntityStatus.DELETED)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task Delete(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                comment.Status = EntityStatus.DELETED;
                _context.Comments.Update(comment);
                await _context.SaveChangesAsync();
            }
        }
    }
}