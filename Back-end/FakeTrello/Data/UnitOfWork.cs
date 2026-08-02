using FakeTrello.Data.Contract;
using FakeTrello.Repository.Contract;
using FakeTrello.Repository;

namespace FakeTrello.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MyDbContext _context;

        public UnitOfWork(MyDbContext context)
        {
            _context = context;
        }

        public async Task BeginTransactionAsync() =>
            await _context.Database.BeginTransactionAsync();

        public async Task CommitAsync() =>
            await _context.Database.CommitTransactionAsync();

        public async Task RollbackAsync() =>
            await _context.Database.RollbackTransactionAsync();

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
