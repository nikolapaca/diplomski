using Pivot.Model;

namespace Pivot.Repository.Contract
{
    public interface ICardImageRepository
    {
        Task<CardImage> Create(CardImage image);
        Task<CardImage?> GetById(int id);
        Task<List<CardImage>> GetByCardId(int cardId);
        Task Delete(int id);
    }
}
