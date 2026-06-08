using FMVideoManagerApi.Models;

namespace FMVideoManagerApi.Data.DTO.Indexing
{
    public sealed class CloudIndexingJobState
    {
        public Guid JobId { get; init; }

        public long UserId { get; init; }

        public long CloudProviderAccountId { get; init; }

        public CloudProviderType Provider { get; init; }

        public CloudIndexingJobStatus Status { get; set; } = CloudIndexingJobStatus.Pending;

        public bool IsIndeterminate { get; set; } = true;

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

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAtUtc { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; } = new();

        public CloudIndexingJobDto ToDto()
        {
            return new CloudIndexingJobDto
            {
                JobId = JobId,
                CloudProviderAccountId = CloudProviderAccountId,
                Provider = Provider,
                Status = Status,
                IsIndeterminate = IsIndeterminate,
                TotalFiles = TotalFiles,
                ProcessedFiles = ProcessedFiles,
                FailedFiles = FailedFiles,
                DownloadedFiles = DownloadedFiles,
                IndexedFiles = IndexedFiles,
                MarkedMissing = MarkedMissing,
                CurrentFileName = CurrentFileName,
                CurrentFilePath = CurrentFilePath,
                StatusMessage = StatusMessage,
                ErrorMessage = ErrorMessage,
                CreatedAtUtc = CreatedAtUtc,
                UpdatedAtUtc = UpdatedAtUtc,
                CompletedAtUtc = CompletedAtUtc
            };
        }
    }
}