namespace FMVideoManagerApi.Data.DTO.Indexing
{
    public sealed class CloudIndexingProgress
    {
        public bool IsIndeterminate { get; init; }

        public int TotalFiles { get; init; }

        public int ProcessedFiles { get; init; }

        public int FailedFiles { get; init; }

        public int DownloadedFiles { get; init; }

        public int IndexedFiles { get; init; }

        public int MarkedMissing { get; init; }

        public string? CurrentFileName { get; init; }

        public string? CurrentFilePath { get; init; }

        public string StatusMessage { get; init; } = string.Empty;
    }
}