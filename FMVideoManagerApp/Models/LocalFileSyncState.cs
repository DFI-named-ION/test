namespace FMVideoManagerApp.Models
{
    public enum LocalFileSyncState
    {
        LocalOnly = 0,
        PendingSync = 1,
        Synced = 2,
        SyncFailed = 3
    }
}