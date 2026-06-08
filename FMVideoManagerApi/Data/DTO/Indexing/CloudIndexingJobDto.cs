using FMVideoManagerApi.Models;

namespace FMVideoManagerApi.Data.DTO.Indexing
{
    public sealed class CloudIndexingJobDto
    {
        public Guid JobId { get; set; }

        public long CloudProviderAccountId { get; set; }

        public CloudProviderType Provider { get; set; }

        public CloudIndexingJobStatus Status { get; set; }

        public bool IsIndeterminate { get; set; }

        public int TotalFiles { get; set; }

        public int ProcessedFiles { get; set; }

        public int FailedFiles { get; set; }

        public int DownloadedFiles { get; set; }

        public int IndexedFiles { get; set; }

        public int MarkedMissing { get; set; }

        public string? CurrentFileName { get; set; }

        public string? CurrentFilePath { get; set; }

        public string StatusMessage { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }
    }
}