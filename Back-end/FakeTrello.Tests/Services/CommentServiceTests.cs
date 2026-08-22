using AutoMapper;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class CommentServiceTests
    {
        private readonly Mock<ICommentRepository> _commentRepository = new();
        private readonly Mock<ICardRepository> _cardRepository = new();
        private readonly Mock<IUserService> _userService = new();
        private readonly Mock<IUserBoardService> _userBoardService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<IMapper> _mapper = new();

        private CommentService CreateService() => new(
            _commentRepository.Object,
            _cardRepository.Object,
            _userService.Object,
            _userBoardService.Object,
            _notificationService.Object,
            _mapper.Object);

        private static Card MakeCard(int id, int boardId, params CardAssignee[] assignees) => new()
        {
            Id = id,
            Name = "Test card",
            CardListId = 1,
            CardList = new CardList { Id = 1, BoardId = boardId, Name = "Test list" },
            Assignees = assignees.ToList()
        };

        [Fact]
        public async Task Create_UserNotBoardMember_ReturnsFail()
        {
            var user = new User { Id = 1, Username = "outsider" };
            var card = MakeCard(id: 5, boardId: 10);

            _userService.Setup(s => s.GetUserByUsername("outsider")).ReturnsAsync(user);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Create(5, "Hello", "outsider");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("don't have access"));
            _commentRepository.Verify(r => r.Create(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Create_AuthorIsAlsoAssignee_DoesNotNotifySelf()
        {
            var author = new User { Id = 1, Username = "member" };
            var selfAssignment = new CardAssignee(5, 1, 10);
            var card = MakeCard(id: 5, boardId: 10, selfAssignment);

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(author);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _commentRepository.Setup(r => r.Create(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

            var service = CreateService();
            var result = await service.Create(5, "Looks good", "member");

            Assert.True(result.IsSuccess);
            _notificationService.Verify(n => n.Create(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task Create_OtherUserIsAssigned_NotifiesAssignee()
        {
            var author = new User { Id = 1, Username = "member" };
            var assignment = new CardAssignee(5, 2, 10); 
            var card = MakeCard(id: 5, boardId: 10, assignment);

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(author);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _commentRepository.Setup(r => r.Create(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

            var service = CreateService();
            var result = await service.Create(5, "Please check this", "member");

            Assert.True(result.IsSuccess);
            _notificationService.Verify(n => n.Create(
                2, 1, NotificationType.NEW_COMMENT_ON_CARD, It.IsAny<string>(), 10, 5), Times.Once);
        }

        [Fact]
        public async Task Delete_NotAuthorNorBoardOwner_ReturnsFail()
        {
            var author = new User { Id = 1, Username = "author" };
            var comment = new Comment { Id = 7, CardId = 5, UserId = 1, User = author, Text = "hi" };
            var card = MakeCard(id: 5, boardId: 10);

            _commentRepository.Setup(r => r.GetById(7)).ReturnsAsync(comment);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("someoneElse", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Delete(7, "someoneElse");

            Assert.True(result.IsFailed);
            _commentRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_BoardOwnerNotAuthor_CanDeleteAnyComment()
        {
            var author = new User { Id = 1, Username = "author" };
            var comment = new Comment { Id = 7, CardId = 5, UserId = 1, User = author, Text = "hi" };
            var card = MakeCard(id: 5, boardId: 10);

            _commentRepository.Setup(r => r.GetById(7)).ReturnsAsync(comment);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);

            var service = CreateService();
            var result = await service.Delete(7, "owner");

            Assert.True(result.IsSuccess);
            _commentRepository.Verify(r => r.Delete(7), Times.Once);
        }
    }
}
