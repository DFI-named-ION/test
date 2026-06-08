namespace FMVideoManagerApp.Data.DTO.Indexing
{
    public sealed class IndexingProgress
    {
        public int TotalFiles { get; init; }

        public int ProcessedFiles { get; init; }

        public int FailedFiles { get; init; }

        public string? CurrentFilePath { get; init; }

        public bool IsIndeterminate { get; init; }

        public string StatusMessage { get; init; } = string.Empty;
    }
}