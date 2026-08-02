using AutoMapper;
using FakeTrello.Hub;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IHubContext<NotificationHub>> _hubContext = new();
        private readonly Mock<IHubClients> _hubClients = new();
        private readonly Mock<IClientProxy> _clientProxy = new();

        public NotificationServiceTests()
        {
            _hubContext.Setup(h => h.Clients).Returns(_hubClients.Object);
            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
        }

        private NotificationService CreateService() => new(
            _notificationRepository.Object,
            _mapper.Object,
            _userRepository.Object,
            _hubContext.Object);

        [Fact]
        public async Task Create_RecipientIsSameAsCreator_ReturnsFailAndDoesNotPersist()
        {
            var service = CreateService();

            var result = await service.Create(
                recipientUserId: 5,
                creatingUserId: 5,
                type: NotificationType.ASSIGNED_TO_CARD,
                message: "irrelevant");

            Assert.True(result.IsFailed);
            _notificationRepository.Verify(r => r.Create(It.IsAny<Notification>()), Times.Never);
            _clientProxy.Verify(
                c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default),
                Times.Never);
        }

        [Fact]
        public async Task Create_RecipientDoesNotExist_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetById(99)).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.Create(
                recipientUserId: 99,
                creatingUserId: 1,
                type: NotificationType.ADDED_TO_BOARD,
                message: "irrelevant");

            Assert.True(result.IsFailed);
            _notificationRepository.Verify(r => r.Create(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task Create_DifferentRecipientAndCreator_PersistsAndPushesToRecipientGroup()
        {
            var recipient = new User { Id = 2, Username = "recipient" };
            _userRepository.Setup(r => r.GetById(2)).ReturnsAsync(recipient);

            Notification createdNotification = null;
            _notificationRepository
                .Setup(r => r.Create(It.IsAny<Notification>()))
                .Callback<Notification>(n => createdNotification = n)
                .ReturnsAsync((Notification n) => n);

            var service = CreateService();
            var result = await service.Create(
                recipientUserId: 2,
                creatingUserId: 1,
                type: NotificationType.ASSIGNED_TO_CARD,
                message: "You were assigned",
                boardId: 10,
                cardId: 20);

            Assert.True(result.IsSuccess);
            Assert.NotNull(createdNotification);
            Assert.Equal(2, createdNotification.RecipientUserId);
            Assert.Equal(1, createdNotification.CreatingUserId);
            Assert.False(createdNotification.IsRead);

            _hubClients.Verify(c => c.Group("user-recipient"), Times.Once);
            _clientProxy.Verify(
                c => c.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), default),
                Times.Once);
        }

        [Fact]
        public async Task GetMyNotifications_UnknownUser_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("ghost")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.GetMyNotifications("ghost");

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetMyNotifications_KnownUser_ReturnsRepositoryResult()
        {
            var user = new User { Id = 1, Username = "member" };
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _notificationRepository.Setup(r => r.GetByRecipientUserId(1))
                .ReturnsAsync(new List<Notification> { new Notification { Id = 1, RecipientUserId = 1 } });

            var service = CreateService();
            var result = await service.GetMyNotifications("member");

            Assert.True(result.IsSuccess);
            _notificationRepository.Verify(r => r.GetByRecipientUserId(1), Times.Once);
        }

        [Fact]
        public async Task GetMyUnreadNotifications_UnknownUser_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("ghost")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.GetMyUnreadNotifications("ghost");

            Assert.True(result.IsFailed);
            _notificationRepository.Verify(
                r => r.GetUnreadByRecipientUserId(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetMyUnreadNotifications_KnownUser_ReturnsOnlyUnread()
        {
            var user = new User { Id = 1, Username = "member" };
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _notificationRepository.Setup(r => r.GetUnreadByRecipientUserId(1))
                .ReturnsAsync(new List<Notification> { new Notification { Id = 1, RecipientUserId = 1, IsRead = false } });

            var service = CreateService();
            var result = await service.GetMyUnreadNotifications("member");

            Assert.True(result.IsSuccess);
            _notificationRepository.Verify(r => r.GetUnreadByRecipientUserId(1), Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_UnknownUser_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("ghost")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.MarkAsRead(1, "ghost");

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task MarkAsRead_NotificationDoesNotExist_ReturnsFail()
        {
            var user = new User { Id = 1, Username = "member" };
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _notificationRepository.Setup(r => r.GetById(5)).ReturnsAsync((Notification)null);

            var service = CreateService();
            var result = await service.MarkAsRead(5, "member");

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task MarkAsRead_NotificationBelongsToSomeoneElse_ReturnsFail()
        {
            var user = new User { Id = 1, Username = "member" };
            var notification = new Notification { Id = 5, RecipientUserId = 99 };
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _notificationRepository.Setup(r => r.GetById(5)).ReturnsAsync(notification);

            var service = CreateService();
            var result = await service.MarkAsRead(5, "member");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("cannot update"));
            _notificationRepository.Verify(r => r.Update(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsRead_OwnNotification_MarksItRead()
        {
            var user = new User { Id = 1, Username = "member" };
            var notification = new Notification { Id = 5, RecipientUserId = 1, IsRead = false };
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _notificationRepository.Setup(r => r.GetById(5)).ReturnsAsync(notification);

            var service = CreateService();
            var result = await service.MarkAsRead(5, "member");

            Assert.True(result.IsSuccess);
            Assert.True(notification.IsRead);
            _notificationRepository.Verify(r => r.Update(notification), Times.Once);
        }

        [Fact]
        public async Task MarkAllAsRead_UnknownUser_ReturnsFail()
        {
            _userRepository.Setup(r => r.GetByUsername("ghost")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.MarkAllAsRead("ghost");

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task MarkAllAsRead_MarksEveryUnreadNotification()
        {
            var user = new User { Id = 1, Username = "member" };
            var unread1 = new Notification { Id = 1, RecipientUserId = 1, IsRead = false };
            var unread2 = new Notification { Id = 2, RecipientUserId = 1, IsRead = false };

            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _notificationRepository.Setup(r => r.GetUnreadByRecipientUserId(1))
                .ReturnsAsync(new List<Notification> { unread1, unread2 });

            var service = CreateService();
            var result = await service.MarkAllAsRead("member");

            Assert.True(result.IsSuccess);
            Assert.True(unread1.IsRead);
            Assert.True(unread2.IsRead);
            _notificationRepository.Verify(r => r.Update(unread1), Times.Once);
            _notificationRepository.Verify(r => r.Update(unread2), Times.Once);
        }
    }
}