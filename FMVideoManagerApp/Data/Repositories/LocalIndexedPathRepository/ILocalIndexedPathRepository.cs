using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.Data.Repositories.LocalIndexedPathRepository
{
    public interface ILocalIndexedPathRepository
    {
        List<LocalIndexedPath> GetByUserId(long serverUserId);

        LocalIndexedPath? FindByPath(long serverUserId, string path);

        LocalIndexedPath Add(LocalIndexedPath path);

        void Remove(long id, long serverUserId);

        void SetEnabled(long id, long serverUserId, bool isEnabled);

        void UpdateLastScannedAt(long id, long serverUserId, DateTime scannedAtUtc);
    }
}