using AutoMapper;
using FakeTrello.Data.Contract;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Model.Enum;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class CardServiceTests
    {
        private readonly Mock<ICardRepository> _cardRepository = new();
        private readonly Mock<ICardListService> _cardListService = new();
        private readonly Mock<IUserService> _userService = new();
        private readonly Mock<ICardAssigneeService> _cardAssigneeService = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<IBoardActivityService> _boardActivityService = new();
        private readonly Mock<IBoardService> _boardService = new();
        private readonly Mock<IUserBoardService> _userBoardService = new();

        private CardService CreateService() => new(
            _cardRepository.Object,
            _mapper.Object,
            _cardListService.Object,
            _userService.Object,
            _cardAssigneeService.Object,
            _unitOfWork.Object,
            _notificationService.Object,
            _boardActivityService.Object,
            _boardService.Object,
            _userBoardService.Object);

        private static Card MakeCard(int id, int boardId, int cardListId = 1, int index = 1, bool isPinned = false) => new()
        {
            Id = id,
            Name = "Test card",
            CardListId = cardListId,
            Index = index,
            IsPinned = isPinned,
            CardList = new CardList { Id = cardListId, BoardId = boardId, Name = "Test list" }
        };

        [Fact]
        public async Task TogglePin_NonOwner_ReturnsFail()
        {
            var card = MakeCard(id: 5, boardId: 10);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("collaborator", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.TogglePin(5, "collaborator");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Only the board owner"));
            _cardRepository.Verify(r => r.Update(It.IsAny<Card>()), Times.Never);
        }

        [Fact]
        public async Task TogglePin_Owner_TogglesPinState()
        {
            var card = MakeCard(id: 5, boardId: 10, isPinned: false);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);
            _cardRepository.Setup(r => r.Update(card)).ReturnsAsync(card);
            _cardRepository.Setup(r => r.GetByListId(card.CardListId)).ReturnsAsync(new List<Card> { card });
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync((User?)null);

            var service = CreateService();
            var result = await service.TogglePin(5, "owner");

            Assert.True(result.IsSuccess);
            Assert.True(card.IsPinned);
        }

        [Fact]
        public async Task ReorderCardInsideList_UserNotBoardMember_ReturnsFailAndRollsBack()
        {
            var user = new User { Id = 1, Username = "outsider" };
            var card = MakeCard(id: 5, boardId: 10);
            _userService.Setup(s => s.GetUserByUsername("outsider")).ReturnsAsync(user);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.ReorderCardInsideList(5, 2, "outsider");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("don't have access"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task ReorderCardInsideList_ConcurrentEdit_ReturnsFriendlyErrorAndRollsBack()
        {
            var user = new User { Id = 1, Username = "member" };
            var card = MakeCard(id: 5, boardId: 10, cardListId: 1, index: 1);
            var otherCard = MakeCard(id: 6, boardId: 10, cardListId: 1, index: 2);

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardRepository.Setup(r => r.GetByListId(1)).ReturnsAsync(new List<Card> { card, otherCard });

            _cardRepository
                .Setup(r => r.UpdateRangeAsync(It.IsAny<List<Card>>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var service = CreateService();
            var result = await service.ReorderCardInsideList(5, 2, "member");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("changed by someone else"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task Create_ListDoesNotExist_ReturnsFailAndRollsBack()
        {
            _cardListService.Setup(s => s.GetById(1))
                .ReturnsAsync(FluentResults.Result.Fail<CardListDTO>("CardList with the given ID was not found."));

            var service = CreateService();
            var result = await service.Create(1, new CardDTO { Name = "New card" }, userId: 1);

            Assert.True(result.IsFailed);
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _cardRepository.Verify(r => r.Create(It.IsAny<Card>()), Times.Never);
        }

        [Fact]
        public async Task Create_UserNotBoardMember_ReturnsFailAndRollsBack()
        {
            var list = new CardListDTO { Id = 1, Name = "List", BoardName = "Board", BoardUsername = "owner" };
            var board = new Board { Id = 10, Name = "Board" };
            var user = new UserDTO { Username = "outsider", Name = "N", Surname = "S", Email = "e@e.com", Password = "x" };

            _cardListService.Setup(s => s.GetById(1)).ReturnsAsync(FluentResults.Result.Ok(list));
            _userService.Setup(s => s.GetById(1)).ReturnsAsync(FluentResults.Result.Ok(user));
            _boardService.Setup(s => s.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Create(1, new CardDTO { Name = "New card" }, userId: 1);

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("don't have access"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        }

        [Fact]
        public async Task Create_ValidRequest_CreatesCardAtNextIndexAndLogsActivity()
        {
            var list = new CardListDTO { Id = 1, Name = "List", BoardName = "Board", BoardUsername = "owner" };
            var board = new Board { Id = 10, Name = "Board" };
            var user = new UserDTO { Username = "member", Name = "N", Surname = "S", Email = "e@e.com", Password = "x" };

            _cardListService.Setup(s => s.GetById(1)).ReturnsAsync(FluentResults.Result.Ok(list));
            _userService.Setup(s => s.GetById(1)).ReturnsAsync(FluentResults.Result.Ok(user));
            _boardService.Setup(s => s.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardRepository.Setup(r => r.GetMaxIndexForCardAsync(1)).ReturnsAsync(3);

            _mapper.Setup(m => m.Map<CardDTO, Card>(It.IsAny<CardDTO>()))
                .Returns((CardDTO dto) => new Card { Name = dto.Name });
            _mapper.Setup(m => m.Map<Card, CardDTO>(It.IsAny<Card>()))
                .Returns((Card c) => new CardDTO { Id = c.Id, Name = c.Name });

            Card createdCard = null;
            _cardRepository.Setup(r => r.Create(It.IsAny<Card>()))
                .Callback<Card>(c => createdCard = c)
                .ReturnsAsync((Card c) => c);

            var service = CreateService();
            var result = await service.Create(1, new CardDTO { Name = "New card" }, userId: 1);

            Assert.True(result.IsSuccess);
            Assert.NotNull(createdCard);
            Assert.Equal(4, createdCard.Index);
            Assert.Equal(1, createdCard.CreatedByUserId);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.CARD_CREATED, It.IsAny<string>(), It.IsAny<int?>()), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }


        [Fact]
        public async Task AssignCardToUser_SelfAssign_DoesNotSendNotification()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var user = new User { Id = 1, Username = "member" };

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);

            var service = CreateService();
            var result = await service.AssignCardToUser(new CardDTO { Id = 5 }, "member", "member");

            Assert.True(result.IsSuccess);
            _notificationService.Verify(n => n.Create(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
            _cardAssigneeService.Verify(a => a.Create(It.IsAny<CardAssignee>()), Times.Once);
        }

        [Fact]
        public async Task AssignCardToUser_AssignedByAnotherUser_SendsNotification()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var assignee = new User { Id = 2, Username = "assignee" };
            var creator = new User { Id = 1, Username = "owner" };

            _userService.Setup(s => s.GetUserByUsername("assignee")).ReturnsAsync(assignee);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(creator);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("owner", 10)).ReturnsAsync(true);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("assignee", 10)).ReturnsAsync(true);

            var service = CreateService();
            var result = await service.AssignCardToUser(new CardDTO { Id = 5 }, "assignee", "owner");

            Assert.True(result.IsSuccess);
            _notificationService.Verify(n => n.Create(
                2, 1, NotificationType.ASSIGNED_TO_CARD, It.IsAny<string>(), 10, 5), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AssignCardToUser_TargetUserNotBoardMember_ReturnsFailAndRollsBack()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var assignee = new User { Id = 2, Username = "outsider" };
            var creator = new User { Id = 1, Username = "owner" };

            _userService.Setup(s => s.GetUserByUsername("outsider")).ReturnsAsync(assignee);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(creator);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("owner", 10)).ReturnsAsync(true);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.AssignCardToUser(new CardDTO { Id = 5 }, "outsider", "owner");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("not a member"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _cardAssigneeService.Verify(a => a.Create(It.IsAny<CardAssignee>()), Times.Never);
        }

        [Fact]
        public async Task UnassignCardToUser_NotAssigned_ReturnsFailAndRollsBack()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var user = new User { Id = 2, Username = "assignee" };
            var unassigningUser = new User { Id = 1, Username = "owner" };

            _userService.Setup(s => s.GetUserByUsername("assignee")).ReturnsAsync(user);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(unassigningUser);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("owner", 10)).ReturnsAsync(true);
            _cardAssigneeService.Setup(s => s.GetById(5, 2, 10)).ReturnsAsync((CardAssignee)null);

            var service = CreateService();
            var result = await service.UnassignCardToUser(new CardDTO { Id = 5 }, "assignee", "owner");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("not assigned"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        }

        [Fact]
        public async Task UnassignCardToUser_SelfUnassign_DoesNotSendNotification()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var user = new User { Id = 1, Username = "member" };
            var assignment = new CardAssignee(5, 1, 10);

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardAssigneeService.Setup(s => s.GetById(5, 1, 10)).ReturnsAsync(assignment);

            var service = CreateService();
            var result = await service.UnassignCardToUser(new CardDTO { Id = 5 }, "member", "member");

            Assert.True(result.IsSuccess);
            _notificationService.Verify(n => n.Create(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
            _cardAssigneeService.Verify(a => a.Delete(5, 1, 10), Times.Once);
        }

        [Fact]
        public async Task UnassignCardToUser_ByAnotherUser_SendsNotification()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var user = new User { Id = 2, Username = "assignee" };
            var unassigningUser = new User { Id = 1, Username = "owner" };
            var assignment = new CardAssignee(5, 2, 10);

            _userService.Setup(s => s.GetUserByUsername("assignee")).ReturnsAsync(user);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(unassigningUser);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("owner", 10)).ReturnsAsync(true);
            _cardAssigneeService.Setup(s => s.GetById(5, 2, 10)).ReturnsAsync(assignment);

            var service = CreateService();
            var result = await service.UnassignCardToUser(new CardDTO { Id = 5 }, "assignee", "owner");

            Assert.True(result.IsSuccess);
            _notificationService.Verify(n => n.Create(
                2, 1, NotificationType.UNASSIGNED_FROM_CARD, It.IsAny<string>(), 10, 5), Times.Once);
        }

        [Fact]
        public async Task ReorderCardOutsideList_TargetListOnDifferentBoard_ReturnsFailAndRollsBack()
        {
            var user = new User { Id = 1, Username = "member" };
            var card = MakeCard(id: 5, boardId: 10, cardListId: 1);
            var targetList = new CardListDTO { Id = 2, Name = "Other list" };

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardListService.Setup(s => s.GetById(2)).ReturnsAsync(FluentResults.Result.Ok(targetList));
            _cardListService.Setup(s => s.GetBoardIdByListId(2)).ReturnsAsync(99);

            var service = CreateService();
            var result = await service.ReorderCardOutsideList(5, 2, 1, "member");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("different board"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        }

        [Fact]
        public async Task ReorderCardOutsideList_ValidMove_MovesCardAndLogsActivity()
        {
            var user = new User { Id = 1, Username = "member" };
            var card = MakeCard(id: 5, boardId: 10, cardListId: 1, index: 1);
            var targetList = new CardListDTO { Id = 2, Name = "Target list" };
            var otherCardInOldList = MakeCard(id: 6, boardId: 10, cardListId: 1, index: 2);
            var existingCardInTargetList = MakeCard(id: 7, boardId: 10, cardListId: 2, index: 1);

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardListService.Setup(s => s.GetById(2)).ReturnsAsync(FluentResults.Result.Ok(targetList));
            _cardListService.Setup(s => s.GetBoardIdByListId(2)).ReturnsAsync(10);
            _cardRepository.Setup(r => r.GetByListId(1)).ReturnsAsync(new List<Card> { card, otherCardInOldList });
            _cardRepository.Setup(r => r.GetByListId(2)).ReturnsAsync(new List<Card> { existingCardInTargetList });

            var service = CreateService();
            var result = await service.ReorderCardOutsideList(5, 2, 1, "member");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, card.CardListId);
            Assert.Equal(1, card.Index);
            Assert.Equal(2, existingCardInTargetList.Index);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.CARD_MOVED, It.IsAny<string>(), 5), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task Update_UserNotBoardMember_ReturnsFail()
        {
            var card = MakeCard(id: 5, boardId: 10);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Update(new CardDTO { Id = 5, Name = "Renamed" }, "outsider");

            Assert.True(result.IsFailed);
            _cardRepository.Verify(r => r.Update(It.IsAny<Card>()), Times.Never);
        }

        [Fact]
        public async Task Update_ValidRequest_UpdatesFieldsAndLogsActivity()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var user = new User { Id = 1, Username = "member" };

            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardRepository.Setup(r => r.Update(card)).ReturnsAsync(card);
            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _mapper.Setup(m => m.Map<Card, CardDTO>(It.IsAny<Card>()))
                .Returns((Card c) => new CardDTO { Id = c.Id, Name = c.Name, Description = c.Description });

            var service = CreateService();
            var result = await service.Update(
                new CardDTO { Id = 5, Name = "Renamed", Description = "New description" }, "member");

            Assert.True(result.IsSuccess);
            Assert.Equal("Renamed", card.Name);
            Assert.Equal("New description", card.Description);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.CARD_UPDATED, It.IsAny<string>(), 5), Times.Once);
        }

        [Fact]
        public async Task Delete_UserNotBoardMember_ReturnsFail()
        {
            var card = MakeCard(id: 5, boardId: 10);
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Delete(5, "outsider");

            Assert.True(result.IsFailed);
            _cardRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_ValidRequest_DeletesCardAndLogsActivity()
        {
            var card = MakeCard(id: 5, boardId: 10);
            var user = new User { Id = 1, Username = "member" };

            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.Delete(5, "member");

            Assert.True(result.IsSuccess);
            _cardRepository.Verify(r => r.Delete(5), Times.Once);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.CARD_DELETED, It.IsAny<string>(), null), Times.Once);
        }

        [Fact]
        public async Task GetUsersAssignedToCard_CardDoesNotExist_ReturnsFail()
        {
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync((Card)null);

            var service = CreateService();
            var result = await service.GetUsersAssignedToCard(5);

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetUsersAssignedToCard_NoAssignees_ReturnsEmptyList()
        {
            var card = MakeCard(id: 5, boardId: 10);
            card.Assignees = new List<CardAssignee>();
            _cardRepository.Setup(r => r.GetById(5)).ReturnsAsync(card);

            var service = CreateService();
            var result = await service.GetUsersAssignedToCard(5);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }
    }
}