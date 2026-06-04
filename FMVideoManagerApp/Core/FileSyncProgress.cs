using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.Core
{
    public sealed class FileSyncProgress
    {
        public int TotalFiles { get; init; }

        public int ProcessedFiles { get; init; }

        public LocalFileLocation? SyncedFile { get; init; }

        public string StatusMessage { get; init; } = string.Empty;

        public bool IsCompleted { get; init; }
    }
}