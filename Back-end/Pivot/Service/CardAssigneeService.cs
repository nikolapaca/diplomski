using Pivot.Model;
using Pivot.Repository.Contract;
using Pivot.Service.Contract;
using System.Reflection.Metadata.Ecma335;

namespace Pivot.Service
{
    public class CardAssigneeService : ICardAssigneeService
    {
        private readonly ICardAssigneeRepository _repository;

        public CardAssigneeService(ICardAssigneeRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CardAssignee>> GetAll()
        {
            return await _repository.GetAll();
        }

        public async Task<List<CardAssignee>> GetByCardId(int cardId)
        {
            return await _repository.GetByCardId(cardId);
        }

        public async Task<CardAssignee> GetById(int cardId, int userId, int boardId)
        {
            return await _repository.GetById(cardId, userId, boardId);
        }

        public async Task<CardAssignee> Create(CardAssignee ca)
        {
            return await _repository.CreateAsync(ca);
        }

        public async Task<CardAssignee> Update(CardAssignee ca)
        {
            return await _repository.UpdateAsync(ca);
        }

        public async Task Delete(int cardId, int userId, int boardId) 
        { 
            await _repository.Delete(cardId, userId, boardId);
        }
    }
}
