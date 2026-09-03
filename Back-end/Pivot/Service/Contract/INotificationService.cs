using Pivot.DTO;
using Pivot.Model;
using Pivot.Model.Enum;
using FluentResults;

namespace Pivot.Service.Contract
{
    public interface INotificationService
    {
        Task<Result<NotificationDTO>> Create(
            int recipientUserId,
            int creatingUserId,
            NotificationType type,
            string message,
            int? boardId = null,
            int? cardId = null
        );

        Task<Result<List<NotificationDTO>>> GetMyNotifications(string username);

        Task<Result<List<NotificationDTO>>> GetMyUnreadNotifications(string username);

        Task<Result> MarkAsRead(int notificationId, string username);

        Task<Result> MarkAllAsRead(string username);
    }
}
