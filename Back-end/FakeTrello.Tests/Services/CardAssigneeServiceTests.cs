using FakeTrello.Model;
using FakeTrello.Repository.Contract;
using FakeTrello.Service;
using Moq;
using Xunit;

namespace FakeTrello.Tests.Services
{
    public class CardAssigneeServiceTests
    {
        private readonly Mock<ICardAssigneeRepository> _repository = new();

        private CardAssigneeService CreateService() => new(_repository.Object);

        [Fact]
        public async Task GetAll_ReturnsAllFromRepository()
        {
            var assignees = new List<CardAssignee> { new(1, 2, 3), new(4, 5, 6) };
            _repository.Setup(r => r.GetAll()).ReturnsAsync(assignees);

            var service = CreateService();
            var result = await service.GetAll();

            Assert.Equal(assignees, result);
        }

        [Fact]
        public async Task GetByCardId_ReturnsAssigneesForThatCard()
        {
            var assignees = new List<CardAssignee> { new(5, 1, 10) };
            _repository.Setup(r => r.GetByCardId(5)).ReturnsAsync(assignees);

            var service = CreateService();
            var result = await service.GetByCardId(5);

            Assert.Equal(assignees, result);
        }

        [Fact]
        public async Task GetById_ReturnsMatchingAssignee()
        {
            var assignee = new CardAssignee(5, 1, 10);
            _repository.Setup(r => r.GetById(5, 1, 10)).ReturnsAsync(assignee);

            var service = CreateService();
            var result = await service.GetById(5, 1, 10);

            Assert.Equal(assignee, result);
        }

        [Fact]
        public async Task GetById_NoMatch_ReturnsNull()
        {
            _repository.Setup(r => r.GetById(5, 1, 10)).ReturnsAsync((CardAssignee)null);

            var service = CreateService();
            var result = await service.GetById(5, 1, 10);

            Assert.Null(result);
        }

        [Fact]
        public async Task Create_DelegatesToRepositoryCreateAsync()
        {
            var assignee = new CardAssignee(5, 1, 10);
            _repository.Setup(r => r.CreateAsync(assignee)).ReturnsAsync(assignee);

            var service = CreateService();
            var result = await service.Create(assignee);

            Assert.Equal(assignee, result);
            _repository.Verify(r => r.CreateAsync(assignee), Times.Once);
        }

        [Fact]
        public async Task Update_DelegatesToRepositoryUpdateAsync()
        {
            var assignee = new CardAssignee(5, 1, 10);
            _repository.Setup(r => r.UpdateAsync(assignee)).ReturnsAsync(assignee);

            var service = CreateService();
            var result = await service.Update(assignee);

            Assert.Equal(assignee, result);
            _repository.Verify(r => r.UpdateAsync(assignee), Times.Once);
        }

        [Fact]
        public async Task Delete_DelegatesToRepositoryDelete()
        {
            var service = CreateService();
            await service.Delete(5, 1, 10);

            _repository.Verify(r => r.Delete(5, 1, 10), Times.Once);
        }
    }
}