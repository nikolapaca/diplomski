using FakeTrello.Data;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly MyDbContext _context;

        public NotificationRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> Create(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<List<Notification>> GetByRecipientUserId(int userId)
        {
            return await _context.Notifications
                .Include(n => n.CreatingUser)
                .Include(n => n.RecipientUser)
                .Include(n => n.Board)
                    .ThenInclude(b => b.UserBoards)
                        .ThenInclude(ub => ub.User)
                .Include(n => n.Card)
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUnreadByRecipientUserId(int userId)
        {
            return await _context.Notifications
                .Include(n => n.CreatingUser)
                .Include(n => n.RecipientUser)
                .Include(n => n.Board)
                    .ThenInclude(b => b.UserBoards)
                        .ThenInclude(ub => ub.User)
                .Include(n => n.Card)
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetById(int id)
        {
            return await _context.Notifications
                .Include(n => n.CreatingUser)
                .Include(n => n.RecipientUser)
                .Include(n => n.Board)
                    .ThenInclude(b => b.UserBoards)
                        .ThenInclude(ub => ub.User)
                .Include(n => n.Card)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Notification> Update(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return notification;
        }
    }
}
