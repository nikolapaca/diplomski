using FakeTrello.Repository.Contract;

namespace FakeTrello.Data.Contract
{
    public interface IUnitOfWork : IDisposable
    {
        IBoardRepository Boards { get; }
        IUserBoardRepository UserBoards { get; }
        ICardListRepository CardLists { get; }
        ICardRepository Cards { get; }

        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task SaveChangesAsync();
    }
}
