using AutoMapper;
using FakeTrello.Data.Contract;
using FakeTrello.DTO;
using FluentResults;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using FakeTrello.Service.Contract;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class CardListServiceTests
    {
        private readonly Mock<ICardListRepository> _cardListRepository = new();
        private readonly Mock<ICardRepository> _cardRepository = new();
        private readonly Mock<IBoardService> _boardService = new();
        private readonly Mock<IUserService> _userService = new();
        private readonly Mock<IUserBoardService> _userBoardService = new();
        private readonly Mock<IBoardActivityService> _boardActivityService = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private CardListService CreateService() => new(
            _cardListRepository.Object,
            _cardRepository.Object,
            _mapper.Object,
            _boardService.Object,
            _unitOfWork.Object,
            _userService.Object,
            _userBoardService.Object,
            _boardActivityService.Object);

        private static CardList MakeList(int id, int boardId, int index = 1, bool isPinned = false) => new()
        {
            Id = id,
            Name = "Test list",
            BoardId = boardId,
            Index = index,
            IsPinned = isPinned
        };

        [Fact]
        public async Task TogglePin_NonOwner_ReturnsFail()
        {
            var list = MakeList(id: 3, boardId: 10);
            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("collaborator", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.TogglePin(3, "collaborator");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Only the board owner"));
            _cardListRepository.Verify(r => r.Update(It.IsAny<CardList>()), Times.Never);
        }

        [Fact]
        public async Task MoveList_UserNotBoardMember_ReturnsFailAndRollsBack()
        {
            var list = MakeList(id: 3, boardId: 10);
            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.MoveList(new CardListDTO { Id = 3 }, 2, "outsider");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("don't have access"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task MoveList_ConcurrentEdit_ReturnsFriendlyErrorAndRollsBack()
        {
            var list = MakeList(id: 3, boardId: 10, index: 1);
            var otherList = MakeList(id: 4, boardId: 10, index: 2);

            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardListRepository.Setup(r => r.GetByBoardId(10)).ReturnsAsync(new List<CardList> { list, otherList });

            // Simulates another user having reordered the same board's lists a moment
            // earlier: EF detects the stale xmin on SaveChanges and throws.
            _cardListRepository
                .Setup(r => r.UpdateRangeAsync(It.IsAny<List<CardList>>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var service = CreateService();
            var result = await service.MoveList(new CardListDTO { Id = 3 }, 2, "member");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("changed by someone else"));
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task MoveList_ValidMove_ShiftsAffectedListsAndSetsNewIndex()
        {
            var list = MakeList(id: 1, boardId: 10, index: 1);
            var list2 = MakeList(id: 2, boardId: 10, index: 2);
            var list3 = MakeList(id: 3, boardId: 10, index: 3);

            _cardListRepository.Setup(r => r.GetById(1)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardListRepository.Setup(r => r.GetByBoardId(10))
                .ReturnsAsync(new List<CardList> { list, list2, list3 });

            var service = CreateService();
            var result = await service.MoveList(new CardListDTO { Id = 1 }, 3, "member");

            Assert.True(result.IsSuccess);
            Assert.Equal(3, list.Index);   // moved list now at target index
            Assert.Equal(1, list2.Index);  // shifted down since it was between old and new index
            Assert.Equal(2, list3.Index);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task MoveList_SameIndex_ReturnsOkWithoutCommitting()
        {
            var list = MakeList(id: 1, boardId: 10, index: 2);

            _cardListRepository.Setup(r => r.GetById(1)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);

            var service = CreateService();
            var result = await service.MoveList(new CardListDTO { Id = 1 }, 2, "member");

            Assert.True(result.IsSuccess);
            _cardListRepository.Verify(r => r.UpdateRangeAsync(It.IsAny<List<CardList>>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task TogglePin_Owner_PinsListAndReindexesBoard()
        {
            var list = MakeList(id: 3, boardId: 10, index: 2, isPinned: false);
            var otherList = MakeList(id: 4, boardId: 10, index: 1);

            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserOwnerOfBoard("owner", 10)).ReturnsAsync(true);
            _cardListRepository.Setup(r => r.Update(list)).ReturnsAsync(list);
            _cardListRepository.Setup(r => r.GetByBoardId(10)).ReturnsAsync(new List<CardList> { otherList, list });
            _userService.Setup(s => s.GetUserByUsername("owner")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.TogglePin(3, "owner");

            Assert.True(result.IsSuccess);
            Assert.True(list.IsPinned);
            Assert.Equal(1, otherList.Index);
            Assert.Equal(2, list.Index);
            _cardListRepository.Verify(r => r.UpdateRangeAsync(It.IsAny<List<CardList>>()), Times.Once);
        }

        [Fact]
        public async Task Create_BoardDoesNotExist_ReturnsFail()
        {
            _boardService.Setup(s => s.GetByNameAndOwnerUsername("Ghost", "owner")).ReturnsAsync((Board)null);

            var service = CreateService();
            var result = await service.Create(
                new CardListDTO { Name = "List", BoardName = "Ghost", BoardUsername = "owner" }, "member");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("Board doesn't exist"));
            _cardListRepository.Verify(r => r.Create(It.IsAny<CardList>()), Times.Never);
        }

        [Fact]
        public async Task Create_UserNotBoardMember_ReturnsFail()
        {
            var board = new Board { Id = 10, Name = "Board" };
            _boardService.Setup(s => s.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Create(
                new CardListDTO { Name = "List", BoardName = "Board", BoardUsername = "owner" }, "outsider");

            Assert.True(result.IsFailed);
            Assert.Contains(result.Errors, e => e.Message.Contains("don't have access"));
        }

        [Fact]
        public async Task Create_ValidRequest_AssignsNextIndexAndLogsActivity()
        {
            var board = new Board { Id = 10, Name = "Board" };
            var user = new User { Id = 1, Username = "member" };

            _boardService.Setup(s => s.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardListRepository.Setup(r => r.GetMaxIndexForCardListAsync(10)).ReturnsAsync(2);
            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);
            _mapper.Setup(m => m.Map<CardListDTO, CardList>(It.IsAny<CardListDTO>()))
                .Returns((CardListDTO dto) => new CardList { Name = dto.Name });

            CardList createdList = null;
            _cardListRepository.Setup(r => r.Create(It.IsAny<CardList>()))
                .Callback<CardList>(l => createdList = l)
                .ReturnsAsync((CardList l) => l);

            var service = CreateService();
            var result = await service.Create(
                new CardListDTO { Name = "New list", BoardName = "Board", BoardUsername = "owner" }, "member");

            Assert.True(result.IsSuccess);
            Assert.NotNull(createdList);
            Assert.Equal(3, createdList.Index);
            Assert.Equal(10, createdList.BoardId);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.LIST_CREATED, It.IsAny<string>(), null), Times.Once);
        }

        [Fact]
        public async Task Update_UserNotBoardMember_ReturnsFail()
        {
            var list = MakeList(id: 3, boardId: 10);
            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Update(new CardListDTO { Id = 3, Name = "Renamed" }, "outsider");

            Assert.True(result.IsFailed);
            _cardListRepository.Verify(r => r.Update(It.IsAny<CardList>()), Times.Never);
        }

        [Fact]
        public async Task Update_ValidRequest_RenamesListAndLogsActivity()
        {
            var list = MakeList(id: 3, boardId: 10);
            var user = new User { Id = 1, Username = "member" };

            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _cardListRepository.Setup(r => r.Update(list)).ReturnsAsync(list);
            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.Update(new CardListDTO { Id = 3, Name = "Renamed" }, "member");

            Assert.True(result.IsSuccess);
            Assert.Equal("Renamed", list.Name);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.LIST_UPDATED, It.IsAny<string>(), null), Times.Once);
        }

        [Fact]
        public async Task Delete_UserNotBoardMember_ReturnsFailAndRollsBack()
        {
            var list = MakeList(id: 3, boardId: 10);
            list.Cards = new List<Card>();

            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            // Delete() first calls the service's own GetById(), which maps the entity to a
            // DTO and probes the board — these must be wired or the mapper mock returns
            // null and the method throws before reaching the authorization check.
            _mapper.Setup(m => m.Map<CardList, CardListDTO>(list))
                .Returns(new CardListDTO { Id = list.Id, Name = list.Name, Cards = new List<CardDTO>() });
            _boardService.Setup(s => s.GetById(10)).ReturnsAsync(Result.Fail<BoardDTO>("n/a"));
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("outsider", 10)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.Delete(3, "outsider");

            Assert.True(result.IsFailed);
            _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _cardListRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_ValidRequest_CascadesToCardsAndLogsActivity()
        {
            var card1 = new Card { Id = 100, Name = "Card 1" };
            var list = MakeList(id: 3, boardId: 10);
            list.Cards = new List<Card> { card1 };
            var user = new User { Id = 1, Username = "member" };

            _cardListRepository.Setup(r => r.GetById(3)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<CardList, CardListDTO>(list))
                .Returns(new CardListDTO
                {
                    Id = list.Id,
                    Name = list.Name,
                    Cards = new List<CardDTO> { new CardDTO { Id = 100, Name = "Card 1" } }
                });
            _boardService.Setup(s => s.GetById(10)).ReturnsAsync(Result.Fail<BoardDTO>("n/a"));
            _userBoardService.Setup(s => s.IsUserMemberOfBoard("member", 10)).ReturnsAsync(true);
            _userService.Setup(s => s.GetUserByUsername("member")).ReturnsAsync(user);

            var service = CreateService();
            var result = await service.Delete(3, "member");

            Assert.True(result.IsSuccess);
            _cardRepository.Verify(r => r.Delete(100), Times.Once);
            _cardListRepository.Verify(r => r.Delete(3), Times.Once);
            _boardActivityService.Verify(a => a.Create(
                10, 1, ActivityType.LIST_DELETED, It.IsAny<string>(), null), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}