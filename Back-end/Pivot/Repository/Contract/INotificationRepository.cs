using Pivot.Model;

namespace Pivot.Repository.Contract
{
    public interface INotificationRepository
    {
        Task<Notification> Create(Notification notification);
        Task<List<Notification>> GetByRecipientUserId(int userId);
        Task<List<Notification>> GetUnreadByRecipientUserId(int userId);
        Task<Notification?> GetById(int id);
        Task<Notification> Update(Notification notification);
    }
}
