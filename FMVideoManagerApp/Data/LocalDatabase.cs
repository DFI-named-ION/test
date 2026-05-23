using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace FMVideoManagerApp.Data
{
    public static class LocalDatabase
    {
        public static string GetConnectionString()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string directory = Path.Combine(appData, "FM");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, "FM_local.db");

            return $"Data Source={path};Foreign Keys=True";
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            IDbContextFactory<LocalDbContext> factory =
                serviceProvider.GetRequiredService<IDbContextFactory<LocalDbContext>>();

            using LocalDbContext db = factory.CreateDbContext();

            db.Database.Migrate();
        }
    }
}