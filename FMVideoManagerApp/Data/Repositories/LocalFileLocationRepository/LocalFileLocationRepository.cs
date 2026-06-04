using FMVideoManagerApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FMVideoManagerApp.Data.Repositories.LocalFileLocationRepository
{
    public sealed class LocalFileLocationRepository : ILocalFileLocationRepository
    {
        private readonly IDbContextFactory<LocalDbContext> _contextFactory;

        public LocalFileLocationRepository(IDbContextFactory<LocalDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public List<LocalFileLocation> GetByUserId(long serverUserId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            return db.LocalFileLocations
                .AsNoTracking()
                .Where(x => x.ServerUserId == serverUserId && x.ExistsOnDisk)
                .OrderBy(x => x.Filename)
                .ToList();
        }

        public LocalFileLocation? FindByPath(long serverUserId, string path)
        {
            string normalizedPath = NormalizePath(path);

            using LocalDbContext db = _contextFactory.CreateDbContext();

            return db.LocalFileLocations
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.ServerUserId == serverUserId &&
                    x.Path == normalizedPath);
        }

        public LocalFileLocation Upsert(LocalFileLocation file)
        {
            file.Path = NormalizePath(file.Path);

            using LocalDbContext db = _contextFactory.CreateDbContext();

            LocalFileLocation? existing = db.LocalFileLocations
                .FirstOrDefault(x =>
                    x.ServerUserId == file.ServerUserId &&
                    x.Path == file.Path);

            if (existing == null)
            {
                file.SyncState = LocalFileSyncState.PendingSync;
                db.LocalFileLocations.Add(file);
                db.SaveChanges();
                return file;
            }

            existing.ContentHash = file.ContentHash;
            existing.LocalIndexedPathId = file.LocalIndexedPathId;
            existing.Filename = file.Filename;
            existing.SizeBytes = file.SizeBytes;
            existing.LastModifiedUtc = file.LastModifiedUtc;
            existing.LastSeenUtc = file.LastSeenUtc;
            existing.ExistsOnDisk = true;

            if (existing.ServerFileItemId == null)
            {
                existing.SyncState = LocalFileSyncState.PendingSync;
            }

            db.SaveChanges();

            return existing;
        }

        public void MarkMissingForIndexedPath(long serverUserId, long localIndexedPathId, DateTime scanStartedAtUtc)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            List<LocalFileLocation> missingFiles = db.LocalFileLocations
                .Where(x =>
                    x.ServerUserId == serverUserId &&
                    x.LocalIndexedPathId == localIndexedPathId &&
                    x.LastSeenUtc < scanStartedAtUtc)
                .ToList();

            foreach (LocalFileLocation file in missingFiles)
            {
                file.ExistsOnDisk = false;
            }

            db.SaveChanges();
        }

        public void RemoveAllByUserId(long serverUserId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            var list = db.LocalFileLocations.Where(x => x.ServerUserId == serverUserId).ToList();
            foreach (var it in list)
            {
                db.LocalFileLocations.Remove(it);
            }

            db.SaveChanges();
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path);
        }

        public void MarkSynced(long id, long serverUserId, long serverFileItemId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            LocalFileLocation? file = db.LocalFileLocations
                .FirstOrDefault(x => x.Id == id && x.ServerUserId == serverUserId);

            if (file == null)
                return;

            file.ServerFileItemId = serverFileItemId;
            file.SyncState = LocalFileSyncState.Synced;
            file.LastSyncedAtUtc = DateTime.UtcNow;
            file.LastSyncError = null;

            db.SaveChanges();
        }

        public void MarkSyncFailed(long id, long serverUserId, string error)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            LocalFileLocation? file = db.LocalFileLocations
                .FirstOrDefault(x => x.Id == id && x.ServerUserId == serverUserId);

            if (file == null)
                return;

            file.SyncState = LocalFileSyncState.SyncFailed;
            file.LastSyncError = error;
            file.LastSyncedAtUtc = null;

            db.SaveChanges();
        }

        public List<LocalFileLocation> GetPendingSyncFiles(long serverUserId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            return db.LocalFileLocations
                .AsNoTracking()
                .Where(x =>
                    x.ServerUserId == serverUserId &&
                    x.ExistsOnDisk &&
                    x.ContentHash != null &&
                    (
                        x.SyncState == LocalFileSyncState.PendingSync ||
                        x.SyncState == LocalFileSyncState.SyncFailed ||
                        x.ServerFileItemId == null
                    ))
                .OrderBy(x => x.Filename)
                .ToList();
        }
    }
}