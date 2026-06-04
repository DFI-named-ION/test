using FMVideoManagerApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FMVideoManagerApp.Data.Repositories.LocalIndexedPathRepository
{
    public sealed class LocalIndexedPathRepository : ILocalIndexedPathRepository
    {
        private readonly IDbContextFactory<LocalDbContext> _contextFactory;

        public LocalIndexedPathRepository(IDbContextFactory<LocalDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public List<LocalIndexedPath> GetByUserId(long serverUserId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            return db.LocalIndexedPaths
                .AsNoTracking()
                .Where(x => x.ServerUserId == serverUserId)
                .OrderBy(x => x.Path)
                .ToList();
        }

        public LocalIndexedPath? FindByPath(long serverUserId, string path)
        {
            string normalizedPath = NormalizePath(path);

            using LocalDbContext db = _contextFactory.CreateDbContext();

            return db.LocalIndexedPaths
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.ServerUserId == serverUserId &&
                    x.Path == normalizedPath);
        }

        public LocalIndexedPath Add(LocalIndexedPath path)
        {
            path.Path = NormalizePath(path.Path);
            path.CreatedAtUtc = DateTime.UtcNow;

            using LocalDbContext db = _contextFactory.CreateDbContext();

            db.LocalIndexedPaths.Add(path);
            db.SaveChanges();

            return path;
        }

        public void Remove(long id, long serverUserId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            LocalIndexedPath? path = db.LocalIndexedPaths
                .FirstOrDefault(x => x.Id == id && x.ServerUserId == serverUserId);

            if (path == null)
                return;

            db.LocalIndexedPaths.Remove(path);
            db.SaveChanges();
        }

        public void SetEnabled(long id, long serverUserId, bool isEnabled)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            LocalIndexedPath? path = db.LocalIndexedPaths
                .FirstOrDefault(x => x.Id == id && x.ServerUserId == serverUserId);

            if (path == null)
                return;

            path.IsEnabled = isEnabled;

            db.SaveChanges();
        }

        public void UpdateLastScannedAt(long id, long serverUserId, DateTime scannedAtUtc)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            LocalIndexedPath? path = db.LocalIndexedPaths
                .FirstOrDefault(x => x.Id == id && x.ServerUserId == serverUserId);

            if (path == null)
                return;

            path.LastScannedAtUtc = scannedAtUtc;

            db.SaveChanges();
        }

        private static string NormalizePath(string path)
        {
            string fullPath = Path.GetFullPath(path);

            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}