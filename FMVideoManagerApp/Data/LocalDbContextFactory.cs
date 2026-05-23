using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FMVideoManagerApp.Data
{
    public sealed class LocalDbContextFactory : IDesignTimeDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<LocalDbContext>()
                .UseSqlite("Data Source=local-dev.db;Foreign Keys=True")
                .Options;

            return new LocalDbContext(options);
        }
    }
}