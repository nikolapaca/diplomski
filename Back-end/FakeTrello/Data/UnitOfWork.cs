using FakeTrello.Data.Contract;
using FakeTrello.Repository.Contract;
using FakeTrello.Repository;

namespace FakeTrello.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MyDbContext _context;

        public IBoardRepository Boards { get; private set; }
        public IUserBoardRepository UserBoards { get; private set; }
        public ICardListRepository CardLists { get; private set; }
        public ICardRepository Cards { get; private set; }
        public ICardAssigneeRepository CardAssignees {  get; private set; }

        public UnitOfWork(MyDbContext context)
        {
            _context = context;
            Boards = new BoardRepository(_context);
            UserBoards = new UserBoardRepository(_context);
            CardLists = new CardListRepository(_context);
            Cards = new CardRepository(_context);
            CardAssignees = new CardAssigneeRepository(_context);
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
