using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FakeTrello.Data
{
    public class MyDbContextFactory : IDesignTimeDbContextFactory<MyDbContext> {

        public MyDbContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "FakeTrello")).
                AddJsonFile("appsettings.json").Build();

            var builder = new DbContextOptionsBuilder<MyDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseNpgsql(connectionString);

            return new MyDbContext(builder.Options);
        }
    }
}
