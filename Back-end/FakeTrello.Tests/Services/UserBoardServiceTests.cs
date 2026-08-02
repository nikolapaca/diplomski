using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class UserBoardServiceTests
    {
        private readonly Mock<IUserBoardRepository> _userBoardRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        private UserBoardService CreateService() => new(
            _userBoardRepository.Object,
            _userRepository.Object);

        [Fact]
        public async Task IsUserMemberOfBoard_EmptyUsername_ReturnsFalse()
        {
            var service = CreateService();

            var result = await service.IsUserMemberOfBoard("", 10);

            Assert.False(result);
            _userRepository.Verify(r => r.GetByUsername(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task IsUserMemberOfBoard_UnknownUser_ReturnsFalse()
        {
            _userRepository.Setup(r => r.GetByUsername("ghost")).ReturnsAsync((User)null);

            var service = CreateService();
            var result = await service.IsUserMemberOfBoard("ghost", 10);

            Assert.False(result);
            _userBoardRepository.Verify(r => r.GetByUserAndBoardId(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task IsUserMemberOfBoard_UserHasNoMembership_ReturnsFalse()
        {
            var user = new User { Id = 1, Username = "outsider" };
            _userRepository.Setup(r => r.GetByUsername("outsider")).ReturnsAsync(user);
            _userBoardRepository.Setup(r => r.GetByUserAndBoardId(1, 10)).ReturnsAsync((UserBoard)null);

            var service = CreateService();
            var result = await service.IsUserMemberOfBoard("outsider", 10);

            Assert.False(result);
        }

        [Fact]
        public async Task IsUserMemberOfBoard_UserIsMember_ReturnsTrue()
        {
            var user = new User { Id = 1, Username = "member" };
            _userRepository.Setup(r => r.GetByUsername("member")).ReturnsAsync(user);
            _userBoardRepository.Setup(r => r.GetByUserAndBoardId(1, 10))
                .ReturnsAsync(new UserBoard(1, 10, UserRole.COLLABORATOR));

            var service = CreateService();
            var result = await service.IsUserMemberOfBoard("member", 10);

            Assert.True(result);
        }

        [Fact]
        public async Task IsUserOwnerOfBoard_CollaboratorRole_ReturnsFalse()
        {
            var user = new User { Id = 1, Username = "collaborator" };
            _userRepository.Setup(r => r.GetByUsername("collaborator")).ReturnsAsync(user);
            _userBoardRepository.Setup(r => r.GetByUserAndBoardId(1, 10))
                .ReturnsAsync(new UserBoard(1, 10, UserRole.COLLABORATOR));

            var service = CreateService();
            var result = await service.IsUserOwnerOfBoard("collaborator", 10);

            Assert.False(result);
        }

        [Fact]
        public async Task IsUserOwnerOfBoard_OwnerRole_ReturnsTrue()
        {
            var user = new User { Id = 1, Username = "owner" };
            _userRepository.Setup(r => r.GetByUsername("owner")).ReturnsAsync(user);
            _userBoardRepository.Setup(r => r.GetByUserAndBoardId(1, 10))
                .ReturnsAsync(new UserBoard(1, 10, UserRole.OWNER));

            var service = CreateService();
            var result = await service.IsUserOwnerOfBoard("owner", 10);

            Assert.True(result);
        }

        [Fact]
        public async Task IsUserOwnerOfBoard_NotAMember_ReturnsFalse()
        {
            var user = new User { Id = 1, Username = "outsider" };
            _userRepository.Setup(r => r.GetByUsername("outsider")).ReturnsAsync(user);
            _userBoardRepository.Setup(r => r.GetByUserAndBoardId(1, 10)).ReturnsAsync((UserBoard)null);

            var service = CreateService();
            var result = await service.IsUserOwnerOfBoard("outsider", 10);

            Assert.False(result);
        }
    }
}
