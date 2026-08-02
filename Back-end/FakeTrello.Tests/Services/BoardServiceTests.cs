using AutoMapper;
using FakeTrello.Data.Contract;
using FakeTrello.DTO;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class BoardServiceTests
    {
        private readonly Mock<IBoardRepository> _boardRepository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserService> _userService = new();
        private readonly Mock<IUserBoardService> _userBoardService = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<IBoardActivityService> _boardActivityService = new();
        private readonly Mock<ICardRepository> _cardRepository = new();
        private readonly Mock<ICardListRepository> _cardListRepository = new();

        private BoardService CreateService() => new(
            _boardRepository.Object,
            _mapper.Object,
            _userService.Object,
            _userBoardService.Object,
            _unitOfWork.Object,
            _notificationService.Object,
            _boardActivityService.Object,
            _cardRepository.Object,
            _cardListRepository.Object);

        private static Board MakeBoard(int id, string name = "Board", string ownerUsername = "owner") => new()
        {
            Id = id,
            Name = name,
            UserBoards = new List<UserBoard>()
        };

        [Fact]
        public async Task LeaveBoard_Owner_ReturnsFailAndRollsBack()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 1, Username = "owner" };
            var membership = new UserBoard(1, 10, UserRole.OWNER);

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(user);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(1, 10)).ReturnsAsync(membership);

            var service = CreateService();
            var result = await service.LeaveBoard(new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "owner");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Owner cannot leave"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
            _userBoardService.Verify(s => s.Delete(It.IsAny<UserBoard>()), Times.Never);
        }

        [Fact]
        public async Task LeaveBoard_Collaborator_SucceedsAndCommits()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 2, Username = "collaborator" };
            var membership = new UserBoard(2, 10, UserRole.COLLABORATOR);

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("collaborator")).ReturnsAsync(user);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(2, 10)).ReturnsAsync(membership);

            var service = CreateService();
            var result = await service.LeaveBoard(new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "collaborator");

            Assert.True(result.IsSuccess);
            _userBoardService.Verify(s => s.Delete(membership), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
        }

        [Fact]
        public async Task LeaveBoard_UserNotOnBoard_ReturnsFail()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 3, Username = "stranger" };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("stranger")).ReturnsAsync(user);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(3, 10)).ReturnsAsync((UserBoard)null);

            var service = CreateService();
            var result = await service.LeaveBoard(new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "stranger");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("not on this board"));
        }

        [Fact]
        public async Task Archive_NonOwner_ReturnsFailAndRollsBack()
        {
            var board = MakeBoard(id: 10);

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("collaborator", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Archive("Board", "owner", "collaborator");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Only the owner"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _boardRepository.Verify(r => r.Update(It.IsAny<Board>()), Times.Never);
        }

        [Fact]
        public async Task Archive_Owner_SucceedsAndCommits()
        {
            var board = MakeBoard(id: 10);
            var owner = new User { Id = 1, Username = "owner" };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(owner);
            _boardRepository.Setup(r => r.Update(board)).ReturnsAsync(board);

            var service = CreateService();
            var result = await service.Archive("Board", "owner", "owner");

            Assert.True(result.IsSuccess);
            Assert.Equal(BoardStatus.ARCHIVED, board.Status);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddCollaboratorToBoard_AlreadyMember_ReturnsFailAndRollsBack()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 2, Username = "already-member" };
            var owner = new User { Id = 1, Username = "owner" };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("already-member")).ReturnsAsync(user);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(owner);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(2, 10))
                .ReturnsAsync(new UserBoard(2, 10, UserRole.COLLABORATOR));
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);

            var service = CreateService();
            var result = await service.AddCollaboratorToBoard(
                new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "already-member", "owner");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("already on this board"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        }

        [Fact]
        public async Task AddCollaboratorToBoard_RequestedByNonOwner_ReturnsFailAndRollsBack()
        {
            // Spec 5.b.iv: adding a collaborator is an owner-only action. The requester's
            // identity must come from the authenticated caller, not from a client-supplied
            // OwnerUsername field on the DTO.
            var board = MakeBoard(id: 10);

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("collaborator", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.AddCollaboratorToBoard(
                new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "newuser", "collaborator");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Only the board owner"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _userBoardService.Verify(s => s.Create(It.IsAny<UserBoard>()), Times.Never);
        }

        [Fact]
        public async Task RemoveCollaboratorFromBoard_NotACollaborator_ReturnsFailAndRollsBack()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 2, Username = "outsider" };
            var owner = new User { Id = 1, Username = "owner" };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("outsider")).ReturnsAsync(user);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(owner);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(2, 10)).ReturnsAsync((UserBoard)null);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);

            var service = CreateService();
            var result = await service.RemoveCollaboratorFromBoard(
                new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "outsider", "owner");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("not collaborator"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveCollaboratorFromBoard_RequestedByNonOwner_ReturnsFailAndRollsBack()
        {
            var board = MakeBoard(id: 10);

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("collaborator", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.RemoveCollaboratorFromBoard(
                new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "someone-else", "collaborator");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Only the board owner"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _userBoardService.Verify(s => s.Delete(It.IsAny<UserBoard>()), Times.Never);
        }

        [Fact]
        public async Task Create_DuplicateNameForSameOwner_ReturnsFailAndRollsBack()
        {
            var owner = new User { Id = 1, Username = "owner" };
            var existingBoard = MakeBoard(id: 10, name: "Board");

            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(owner);
            _boardRepository.Setup(r => r.GetByName("Board")).ReturnsAsync(new List<Board> { existingBoard });
            _userBoardService.Setup(s => s.GetByOwnerRoleAndBoardId(10))
                .ReturnsAsync(new UserBoard(1, 10, UserRole.OWNER));

            var service = CreateService();
            var result = await service.Create(new BoardDTO { Name = "Board", OwnerUsername = "owner" }, "owner");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("already exists"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _boardRepository.Verify(r => r.Create(It.IsAny<Board>()), Times.Never);
        }

        [Fact]
        public async Task Delete_ExistingBoard_CascadesToListsAndCardsAndCommits()
        {
            var card1 = new Card { Id = 100, Name = "Card 1" };
            var list = new CardList { Id = 50, Name = "List", Cards = new List<Card> { card1 } };
            var board = MakeBoard(id: 10);
            board.Lists = new List<CardList> { list };
            board.UserBoards = new List<UserBoard>
            {
                new UserBoard(1, 10, UserRole.OWNER),
                new UserBoard(2, 10, UserRole.COLLABORATOR)
            };
            var owner = new User { Id = 1, Username = "owner" };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(owner);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);

            var service = CreateService();
            var result = await service.Delete("Board", "owner", "owner");

            Assert.True(result.IsSuccess);
            _notificationService.Verify(n => n.Create(
                2, 1, NotificationType.BOARD_DELETED, It.IsAny<string>(), 10, null), Times.Once);
            _userBoardService.Verify(s => s.RemoveRange(board.UserBoards), Times.Once);
            _cardRepository.Verify(r => r.Delete(100), Times.Once);
            _cardListRepository.Verify(r => r.Delete(50), Times.Once);
            _boardRepository.Verify(r => r.Delete(10), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
        }

        [Fact]
        public async Task Delete_RequestedByNonOwner_ReturnsFailAndRollsBack()
        {
            // Spec 5.b.ii: deleting a board is an owner-only action; a collaborator
            // must not be able to delete it even if they know the owner's username.
            var board = MakeBoard(id: 10);

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("collaborator", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Delete("Board", "owner", "collaborator");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Only the board owner"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _boardRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Update_RequestedByNonOwner_ReturnsFail()
        {
            // Spec 5.b.i: renaming a board is an owner-only action.
            var board = MakeBoard(id: 10, name: "Old name");

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Old name", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("collaborator", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Update(new BoardUpdateDTO
            {
                OldBoardName = "Old name",
                OwnerUsername = "owner",
                NewBoardName = "New name",
                NewBoardDescription = "New description"
            }, "collaborator");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Only the board owner"));
            _boardRepository.Verify(r => r.Update(It.IsAny<Board>()), Times.Never);
        }

        [Fact]
        public async Task Update_RequestedByOwner_UpdatesNameAndDescriptionAndLogsActivity()
        {
            var board = MakeBoard(id: 10, name: "Old name");
            var owner = new User { Id = 1, Username = "owner" };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Old name", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);
            _boardRepository.Setup(r => r.Update(board)).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync(owner);

            var service = CreateService();
            var result = await service.Update(new BoardUpdateDTO
            {
                OldBoardName = "Old name",
                OwnerUsername = "owner",
                NewBoardName = "New name",
                NewBoardDescription = "New description"
            }, "owner");

            Assert.True(result.IsSuccess);
            Assert.Equal("New name", board.Name);
            Assert.Equal("New description", board.Description);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.BOARD_UPDATED, It.IsAny<string>(), null), Times.Once);
        }

        [Fact]
        public async Task ToggleFavorite_BoardDoesNotExist_ReturnsFail()
        {
            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Ghost", "owner")).ReturnsAsync((Board)null);

            var service = CreateService();
            var result = await service.ToggleFavorite("Ghost", "owner", "member");

            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task ToggleFavorite_UserHasNoMembership_ReturnsFail()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 2, Username = "outsider" };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("outsider")).ReturnsAsync(user);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(2, 10)).ReturnsAsync((UserBoard)null);

            var service = CreateService();
            var result = await service.ToggleFavorite("Board", "owner", "outsider");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("don't have access"));
        }

        [Fact]
        public async Task ToggleFavorite_MemberOfBoard_TogglesFromFalseToTrue()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 2, Username = "member" };
            var membership = new UserBoard(2, 10, UserRole.COLLABORATOR) { IsFavorite = false };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(2, 10)).ReturnsAsync(membership);

            var service = CreateService();
            var result = await service.ToggleFavorite("Board", "owner", "member");

            Assert.True(result.IsSuccess);
            Assert.True(membership.IsFavorite);
            _userBoardService.Verify(s => s.Update(membership), Times.Once);
        }

        [Fact]
        public async Task ToggleFavorite_AlreadyFavorite_TogglesBackToFalse()
        {
            var board = MakeBoard(id: 10);
            var user = new User { Id = 2, Username = "member" };
            var membership = new UserBoard(2, 10, UserRole.COLLABORATOR) { IsFavorite = true };

            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(2, 10)).ReturnsAsync(membership);

            var service = CreateService();
            var result = await service.ToggleFavorite("Board", "owner", "member");

            Assert.True(result.IsSuccess);
            Assert.False(membership.IsFavorite);
        }

        [Fact]
        public async Task GetAllByUsername_ReturnsBoardsWithOwnerAndFavoriteInfo()
        {
            var user = new User { Id = 1, Username = "member" };
            var board = MakeBoard(id: 10, name: "Board");
            var membership = new UserBoard(1, 10, UserRole.COLLABORATOR) { IsFavorite = true };

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _boardRepository.Setup(r => r.GetAllByUserIdOrdered(user)).ReturnsAsync(new List<Board> { board });
            _mapper.Setup(m => m.Map<Board, BoardDTO>(board)).Returns(new BoardDTO { Name = "Board" });
            _userBoardService.Setup(s => s.GetUsernameOfBoardOwner(10)).ReturnsAsync("owner");
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(1, 10)).ReturnsAsync(membership);

            var service = CreateService();
            var result = await service.GetAllByUsername("member");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("owner", result.Value[0].OwnerUsername);
            Assert.True(result.Value[0].IsFavorite);
        }

        [Fact]
        public async Task GetBySearchFilter_UserHasNoMembershipOnMatch_MarksNotFavorite()
        {
            var user = new User { Id = 1, Username = "member" };
            var board = MakeBoard(id: 10, name: "Marketing board");

            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _boardRepository.Setup(r => r.GetBySearchFilter(1, "market"))
                .ReturnsAsync(new List<Board> { board });
            _mapper.Setup(m => m.Map<Board, BoardDTO>(board)).Returns(new BoardDTO { Name = "Marketing board" });
            _userBoardService.Setup(s => s.GetUsernameOfBoardOwner(10)).ReturnsAsync("owner");
            _userBoardService.Setup(s => s.GetByUserIdAndBoardId(1, 10)).ReturnsAsync((UserBoard)null);

            var service = CreateService();
            var result = await service.GetBySearchFilter("member", "market");

            Assert.True(result.IsSuccess);
            Assert.False(result.Value[0].IsFavorite);
        }
    }
}