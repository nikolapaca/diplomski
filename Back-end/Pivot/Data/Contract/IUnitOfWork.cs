namespace Pivot.Data.Contract
{
    public interface IUnitOfWork : IDisposable
    {

        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task SaveChangesAsync();
    }
}
