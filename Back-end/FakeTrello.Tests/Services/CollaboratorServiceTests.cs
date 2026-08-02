using AutoMapper;
using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class CollaboratorServiceTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IBoardRepository> _boardRepository = new();
        private readonly Mock<IMapper> _mapper = new();

        private CollaboratorService CreateService() => new(
            _userRepository.Object,
            _boardRepository.Object,
            _mapper.Object);

        [Fact]
        public async Task GetUsersNotOnBoard_BoardDoesNotExist_ReturnsFail()
        {
            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Ghost", "owner")).ReturnsAsync((Board)null);

            var service = CreateService();
            var result = await service.GetUsersNotOnBoard("search", "Ghost", "owner");

            Assert.True(result.IsFailed);
            _userRepository.Verify(
                r => r.GetUsersNotOnTheBoard(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAssignableUsersOnBoard_BoardDoesNotExist_ReturnsFail()
        {
            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Ghost", "owner")).ReturnsAsync((Board)null);

            var service = CreateService();
            var result = await service.GetAssignableUsersOnBoard("search", "Ghost", "owner", cardId: 5);

            Assert.True(result.IsFailed);
            _userRepository.Verify(
                r => r.GetAssignableUsersOnTheBoard(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetUsersOnBoard_BoardExists_ReturnsUsers()
        {
            var board = new Board { Id = 10, Name = "Board" };
            _boardRepository.Setup(r => r.GetByNameAndOwnerUsername("Board", "owner")).ReturnsAsync(board);
            _userRepository.Setup(r => r.GetUsersOnTheBoard("search", 10))
                .ReturnsAsync(new List<User> { new User { Id = 1, Username = "member" } });

            var service = CreateService();
            var result = await service.GetUsersOnBoard("search", "Board", "owner");

            Assert.True(result.IsSuccess);
        }
    }
}
