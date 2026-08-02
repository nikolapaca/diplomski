using AutoMapper;
using FakeTrello.Hub;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class BoardActivityServiceTests
    {
        private readonly Mock<IBoardActivityRepository> _activityRepository = new();
        private readonly Mock<IBoardRepository> _boardRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IUserBoardService> _userBoardService = new();
        private readonly Mock<ICardRepository> _cardRepository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IHubContext<NotificationHub>> _hubContext = new();
        private readonly Mock<IHubClients> _hubClients = new();
        private readonly Mock<IClientProxy> _clientProxy = new();

        public BoardActivityServiceTests()
        {
            _hubContext.Setup(h => h.Clients).Returns(_hubClients.Object);
            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
        }

        private BoardActivityService CreateService() => new(
            _activityRepository.Object,
            _boardRepository.Object,
            _userRepository.Object,
            _userBoardService.Object,
            _cardRepository.Object,
            _mapper.Object,
            _hubContext.Object);

        [Fact]
        public async Task Create_CreatingUserDoesNotExist_ReturnsFailAndDoesNotPersist()
        {
            _userRepository.Setup(r => r.GetById(1)).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.Create(
                boardId: 10,
                creatingUserId: 1,
                type: ActivityType.CARD_CREATED,
                message: "irrelevant");

            Assert.True(result.IsFailed);
            _activityRepository.Verify(r => r.Create(It.IsAny<BoardActivity>()), Times.Never);
        }

        [Fact]
        public async Task Create_ValidUser_PersistsActivityAndBroadcastsToBoardGroup()
        {
            var user = new User { Id = 1, Username = "member" };
            var board = new Board { Id = 10, Name = "Board" };

            _userRepository.Setup(r => r.GetById(1)).ReturnsAsync(user);
            _activityRepository
                .Setup(r => r.Create(It.IsAny<BoardActivity>()))
                .ReturnsAsync((BoardActivity a) => a);
            _boardRepository.Setup(r => r.GetById(10)).ReturnsAsync(board);
            _userBoardService.Setup(s => s.GetUsernameOfBoardOwner(10)).ReturnsAsync("owner");

            var service = CreateService();
            var result = await service.Create(
                boardId: 10,
                creatingUserId: 1,
                type: ActivityType.CARD_CREATED,
                message: "member created card 'Task 1'.",
                cardId: 99);

            Assert.True(result.IsSuccess);
            _activityRepository.Verify(r => r.Create(It.Is<BoardActivity>(a =>
                a.BoardId == 10 && a.CreatingUserId == 1 && a.CardId == 99)), Times.Once);

            var expectedGroup = NotificationHub.GetBoardGroupName("owner", "Board");
            _hubClients.Verify(c => c.Group(expectedGroup), Times.Once);
            _clientProxy.Verify(
                c => c.SendCoreAsync("ReceiveBoardActivity", It.IsAny<object[]>(), default),
                Times.Once);
        }

        [Fact]
        public async Task Create_BoardNoLongerExists_StillPersistsActivityButSkipsBroadcast()
        {
            var user = new User { Id = 1, Username = "member" };

            _userRepository.Setup(r => r.GetById(1)).ReturnsAsync(user);
            _activityRepository
                .Setup(r => r.Create(It.IsAny<BoardActivity>()))
                .ReturnsAsync((BoardActivity a) => a);
            _boardRepository.Setup(r => r.GetById(10)).ReturnsAsync((Board)null);

            var service = CreateService();
            var result = await service.Create(
                boardId: 10,
                creatingUserId: 1,
                type: ActivityType.BOARD_ARCHIVED,
                message: "irrelevant");

            Assert.True(result.IsSuccess);
            _clientProxy.Verify(
                c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default),
                Times.Never);
        }

        [Fact]
        public async Task GetByBoard_BoardDoesNotExist_ReturnsFail()
        {
            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Ghost", "owner")).ReturnsAsync((Board)null);

            var service = CreateService();
            var result = await service.GetByBoard("Ghost", "owner");

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetByBoard_BoardExists_ReturnsActivities()
        {
            var board = new Board { Id = 10, Name = "Board" };
            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _activityRepository.Setup(r => r.GetByBoardId(10))
                .ReturnsAsync(new List<BoardActivity> { new BoardActivity { Id = 1, BoardId = 10 } });

            var service = CreateService();
            var result = await service.GetByBoard("Board", "owner");

            Assert.True(result.IsSuccess);
            _activityRepository.Verify(r => r.GetByBoardId(10), Times.Once);
        }

        [Fact]
        public async Task GetByCard_CardDoesNotExist_ReturnsFail()
        {
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync((Card)null);

            var service = CreateService();
            var result = await service.GetByCard(5, "member");

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetByCard_UserNotBoardMember_ReturnsFail()
        {
            var card = new Card { Id = 5, CardList = new CardList { Id = 1, BoardId = 10 } };
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.GetByCard(5, "outsider");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("don't have access"));
            _activityRepository.Verify(r => r.GetByCardId(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetByCard_UserIsBoardMember_ReturnsCardHistory()
        {
            var card = new Card { Id = 5, CardList = new CardList { Id = 1, BoardId = 10 } };
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _activityRepository.Setup(r => r.GetByCardId(5))
                .ReturnsAsync(new List<BoardActivity>
                {
                    new BoardActivity { Id = 1, CardId = 5, Type = ActivityType.CARD_CREATED },
                    new BoardActivity { Id = 2, CardId = 5, Type = ActivityType.USER_ASSIGNED_TO_CARD }
                });

            var service = CreateService();
            var result = await service.GetByCard(5, "member");

            Assert.True(result.IsSuccess);
            _activityRepository.Verify(r => r.GetByCardId(5), Times.Once);
        }
    }
}