using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Hub;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service.Contract;
using FluentResults;
using Microsoft.AspNetCore.SignalR;

namespace FakeTrello.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(INotificationRepository notificationRepository, IMapper mapper, IUserRepository userRepository, IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _mapper = mapper;
            _userRepository = userRepository;
            _hubContext = hubContext;
        }

        public async Task<Result<NotificationDTO>> Create(int recipientUserId, int creatingUserId, NotificationType type, string message, int? boardId = null, int? cardId = null
)
        {
            if (recipientUserId == creatingUserId)
            {
                return Result.Fail("User cannot notify himself.");
            }

            var recipientUser = await _userRepository.GetById(recipientUserId);
            if (recipientUser == null)
            {
                return Result.Fail("Recipient user doesn't exist.");
            }

            var notification = new Notification
            {
                RecipientUserId = recipientUserId,
                CreatingUserId = creatingUserId,
                Type = type,
                Message = message,
                BoardId = boardId,
                CardId = cardId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _notificationRepository.Create(notification);

            var notificationDto = _mapper.Map<NotificationDTO>(created);

            await _hubContext.Clients
                .Group($"user-{recipientUser.Username}")
                .SendAsync("ReceiveNotification", notificationDto);

            return Result.Ok(notificationDto);
        }
        public async Task<Result<List<NotificationDTO>>> GetMyNotifications(string username)
        {
            var user = await _userRepository.GetByUsername(username);

            if (user == null)
            {
                return Result.Fail("User not found.");
            }

            var notifications = await _notificationRepository.GetByRecipientUserId(user.Id);

            return Result.Ok(_mapper.Map<List<NotificationDTO>>(notifications));
        }

        public async Task<Result<List<NotificationDTO>>> GetMyUnreadNotifications(string username)
        {
            var user = await _userRepository.GetByUsername(username);

            if (user == null)
            {
                return Result.Fail("User not found.");
            }

            var notifications = await _notificationRepository.GetUnreadByRecipientUserId(user.Id);

            return Result.Ok(_mapper.Map<List<NotificationDTO>>(notifications));
        }

        public async Task<Result> MarkAsRead(int notificationId, string username)
        {
            var user = await _userRepository.GetByUsername(username);

            if (user == null)
            {
                return Result.Fail("User not found.");
            }

            var notification = await _notificationRepository.GetById(notificationId);

            if (notification == null)
            {
                return Result.Fail("Notification not found.");
            }

            if (notification.RecipientUserId != user.Id)
            {
                return Result.Fail("You cannot update this notification.");
            }

            notification.IsRead = true;

            await _notificationRepository.Update(notification);

            return Result.Ok();
        }
    }
}
