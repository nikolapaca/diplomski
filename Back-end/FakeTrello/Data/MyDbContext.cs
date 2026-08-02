using FakeTrello.Model;
using Microsoft.EntityFrameworkCore;

namespace FakeTrello.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<UserBoard> UserBoards { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardList> CardLists { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CardAssignee> CardAssignees { get; set; }
        public DbSet<BoardActivity> BoardActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserBoard>()
                .HasKey(ub => new { ub.UserId, ub.BoardId });

            modelBuilder.Entity<CardAssignee>()
                .HasKey(ca => new { ca.CardId, ca.UserId, ca.BoardId });

            modelBuilder.Entity<CardAssignee>()
                .HasOne(ca => ca.UserBoard)
                .WithMany(ub => ub.AssignedCards)
                .HasForeignKey(ca => new { ca.UserId, ca.BoardId });

            modelBuilder.Entity<CardAssignee>()
                .HasOne(ca => ca.Card)
                .WithMany(c => c.Assignees)
                .HasForeignKey(ca => ca.CardId);
        }

    }
}