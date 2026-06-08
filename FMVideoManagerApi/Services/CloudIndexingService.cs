using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.DTO.Indexing;
using FMVideoManagerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FMVideoManagerApi.Services
{
    public sealed class CloudIndexingJobService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly Dictionary<Guid, CloudIndexingJobState> _jobs = new();

        private readonly object _gate = new();

        public CloudIndexingJobService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Guid StartDropboxIndexing(long userId, long accountId)
        {
            Guid jobId = Guid.NewGuid();

            var job = new CloudIndexingJobState
            {
                JobId = jobId,
                UserId = userId,
                CloudProviderAccountId = accountId,
                Provider = CloudProviderType.Dropbox,
                Status = CloudIndexingJobStatus.Pending,
                IsIndeterminate = true,
                StatusMessage = "Queued..."
            };

            lock (_gate)
            {
                _jobs.Add(jobId, job);
            }

            _ = Task.Run(async () => await RunDropboxIndexingJobAsync(job));

            return jobId;
        }

        public CloudIndexingJobDto? GetJob(long userId, Guid jobId)
        {
            lock (_gate)
            {
                if (!_jobs.TryGetValue(jobId, out CloudIndexingJobState? job))
                    return null; // ?

                if (job.UserId != userId)
                    return null;

                return job.ToDto();
            }
        }

        public CloudIndexingJobDto? GetActiveJob(long userId)
        {
            lock (_gate)
            {
                CloudIndexingJobState? job = _jobs.Values
                    .Where(x =>
                        x.UserId == userId &&
                        !IsTerminalStatus(x.Status))
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefault();

                return job?.ToDto();
            }
        }

        public bool CancelJob(long userId, Guid jobId)
        {
            CloudIndexingJobState? job;

            lock (_gate)
            {
                if (!_jobs.TryGetValue(jobId, out job))
                    return false;

                if (job.UserId != userId)
                    return false;

                if (IsTerminalStatus(job.Status))
                    return true;

                job.Status = CloudIndexingJobStatus.Cancelled;
                job.StatusMessage = "Cloud indexing cancellation requested.";
                job.UpdatedAtUtc = DateTime.UtcNow;
            }

            job.CancellationTokenSource.Cancel();

            return true;
        }

        private async Task RunDropboxIndexingJobAsync(CloudIndexingJobState job)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            ServerDbContext db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            DropboxStorageIndexingService indexingService = scope.ServiceProvider.GetRequiredService<DropboxStorageIndexingService>();

            try
            {
                UpdateJob(job, x =>
                {
                    x.Status = CloudIndexingJobStatus.Running;
                    x.IsIndeterminate = true;
                    x.StatusMessage = "Starting Dropbox indexing...";
                });

                CloudProviderAccount? account = await db.CloudProviderAccounts
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == job.CloudProviderAccountId &&
                            x.UserId == job.UserId &&
                            x.Provider == CloudProviderType.Dropbox,
                        job.CancellationTokenSource.Token);

                if (account == null)
                    throw new InvalidOperationException("Dropbox account not found.");

                if (!account.IsActive)
                    throw new InvalidOperationException("Dropbox account is disabled.");

                var progress = new Progress<CloudIndexingProgress>(p =>
                {
                    UpdateJob(job, x =>
                    {
                        x.IsIndeterminate = p.IsIndeterminate;
                        x.TotalFiles = p.TotalFiles;
                        x.ProcessedFiles = p.ProcessedFiles;
                        x.FailedFiles = p.FailedFiles;
                        x.DownloadedFiles = p.DownloadedFiles;
                        x.IndexedFiles = p.IndexedFiles;
                        x.MarkedMissing = p.MarkedMissing;
                        x.CurrentFileName = p.CurrentFileName;
                        x.CurrentFilePath = p.CurrentFilePath;
                        x.StatusMessage = p.StatusMessage;
                    });
                });

                await indexingService.IndexAsync(account, progress, job.CancellationTokenSource.Token);

                UpdateJob(job, x =>
                {
                    x.Status = CloudIndexingJobStatus.Completed;
                    x.IsIndeterminate = false;
                    x.CurrentFileName = null;
                    x.CurrentFilePath = null;
                    x.StatusMessage = "Cloud indexing completed.";
                    x.CompletedAtUtc = DateTime.UtcNow;
                });
            }
            catch (OperationCanceledException)
            {
                UpdateJob(job, x =>
                {
                    x.Status = CloudIndexingJobStatus.Cancelled;
                    x.IsIndeterminate = false;
                    x.CurrentFileName = null;
                    x.CurrentFilePath = null;
                    x.StatusMessage = "Cloud indexing cancelled.";
                    x.CompletedAtUtc = DateTime.UtcNow;
                });
            }
            catch (Exception ex)
            {
                UpdateJob(job, x =>
                {
                    x.Status = CloudIndexingJobStatus.Failed;
                    x.IsIndeterminate = false;
                    x.ErrorMessage = ex.Message;
                    x.StatusMessage = "Cloud indexing failed.";
                    x.CompletedAtUtc = DateTime.UtcNow;
                });
            }
        }

        private void UpdateJob(
            CloudIndexingJobState job,
            Action<CloudIndexingJobState> update)
        {
            lock (_gate)
            {
                update(job);
                job.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        private static bool IsTerminalStatus(CloudIndexingJobStatus status)
        {
            return status is
                CloudIndexingJobStatus.Completed or
                CloudIndexingJobStatus.Failed or
                CloudIndexingJobStatus.Cancelled;
        }
    }
}