using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.Data.Repositories.LocalFileLocationRepository
{
    public interface ILocalFileLocationRepository
    {
        List<LocalFileLocation> GetByUserId(long serverUserId);

        LocalFileLocation? FindByPath(long serverUserId, string path);

        LocalFileLocation Upsert(LocalFileLocation file);

        void MarkMissingForIndexedPath(long serverUserId, long localIndexedPathId, DateTime scanStartedAtUtc);

        void RemoveAllByUserId(long serverUserId);

        void MarkSynced(long id, long serverUserId, long serverFileItemId);

        void MarkSyncFailed(long id, long serverUserId, string error);
        List<LocalFileLocation> GetPendingSyncFiles(long serverUserId);
    }
}