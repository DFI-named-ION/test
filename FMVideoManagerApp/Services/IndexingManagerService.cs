using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Data.DTO.Indexing;
using System.Windows;

namespace FMVideoManagerApp.Services
{
    public sealed class IndexingManagerService
    {
        private readonly FileIndexingService _localIndexingService;
        private readonly ApiClient _apiClient;

        private CancellationTokenSource? _currentCts;
        private Guid? _currentCloudJobId;

        public FileIndexingState State { get; } = new();

        public bool IsIndexing => State.IsIndexing;

        public IndexingManagerService(FileIndexingService localIndexingService, ApiClient apiClient)
        {
            _localIndexingService = localIndexingService;
            _apiClient = apiClient;
        }

        public async Task StartLocalIndexingAsync(CancellationToken cancellationToken = default)
        {
            if (State.IsIndexing)
                throw new InvalidOperationException("Indexing is already running.");

            _currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            CancellationToken token = _currentCts.Token;

            try
            {
                RunOnUiThread(() =>
                {
                    State.Reset();
                    State.IsIndexing = true;
                    State.StorageName = "Local indexing";
                    State.IsIndeterminate = true;
                    State.StatusMessage = "Starting...";
                });

                var progress = new Progress<IndexingProgress>(ApplyProgress);

                await _localIndexingService.StartIndexingAsync(progress, token);

                RunOnUiThread(() =>
                {
                    State.StatusMessage = "Finished.";
                });
            }
            catch (OperationCanceledException)
            {
                RunOnUiThread(() =>
                {
                    State.StatusMessage = "Cancelled.";
                });
            }
            finally
            {
                FinishIndexing();
            }
        }

        public async Task StartCloudIndexingAsync(CancellationToken cancellationToken = default)
        {
            if (State.IsIndexing)
                throw new InvalidOperationException("Indexing is already running.");

            _currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            CancellationToken token = _currentCts.Token;

            try
            {
                RunOnUiThread(() =>
                {
                    State.Reset();
                    State.IsIndexing = true;
                    State.StorageName = "Cloud indexing";
                    State.IsIndeterminate = true;
                    State.StatusMessage = "Loading cloud accounts...";
                });

                List<CloudProviderAccountDto> accounts = await _apiClient.GetCloudAccountsAsync(token);

                List<CloudProviderAccountDto> activeDropboxAccounts = accounts
                    .Where(x =>
                        x.Provider == CloudProviderType.Dropbox &&
                        x.IsActive)
                    .ToList();

                if (activeDropboxAccounts.Count == 0)
                    throw new InvalidOperationException("No active Dropbox accounts connected.");

                int accountIndex = 0;
                int failedAccounts = 0;

                foreach (CloudProviderAccountDto account in activeDropboxAccounts)
                {
                    token.ThrowIfCancellationRequested();

                    accountIndex++;

                    string accountName =
                        account.DisplayName ??
                        account.Email ??
                        $"Dropbox account {account.Id}";

                    RunOnUiThread(() =>
                    {
                        State.StorageName = $"Dropbox {accountIndex}/{activeDropboxAccounts.Count}";
                        State.IsIndeterminate = true;
                        State.TotalFiles = 0;
                        State.ProcessedFiles = 0;
                        State.FailedFiles = 0;
                        State.CurrentFilePath = null;
                        State.StatusMessage = $"Starting Dropbox indexing for {accountName}...";
                    });

                    try
                    {
                        Guid jobId = await _apiClient.StartDropboxIndexingAsync(account.Id, token);

                        _currentCloudJobId = jobId;

                        await PollCloudIndexingJobAsync(jobId, accountName, accountIndex, activeDropboxAccounts.Count, token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        failedAccounts++;

                        RunOnUiThread(() =>
                        {
                            State.FailedFiles = failedAccounts;
                            State.StatusMessage = $"Failed to index Dropbox account: {accountName}";
                        });
                    }
                    finally
                    {
                        _currentCloudJobId = null;
                    }
                }

                RunOnUiThread(() =>
                {
                    State.CurrentFilePath = null;
                    State.IsIndeterminate = false;
                    State.StatusMessage = failedAccounts == 0
                        ? "Cloud indexing finished."
                        : $"Cloud indexing finished with {failedAccounts} failed account(s).";
                });
            }
            catch (OperationCanceledException)
            {
                RunOnUiThread(() =>
                {
                    State.StatusMessage = "Cloud indexing cancelled.";
                });
            }
            finally
            {
                _currentCloudJobId = null;
                FinishIndexing();
            }
        }

        public async Task CancelIndexingAsync()
        {
            _currentCts?.Cancel();

            Guid? cloudJobId = _currentCloudJobId;

            if (cloudJobId != null)
            {
                try
                {
                    await _apiClient.CancelCloudIndexingJobAsync(cloudJobId.Value);
                }
                catch { }
            }
        }

        private async Task PollCloudIndexingJobAsync(Guid jobId, string accountName, int accountIndex, int totalAccounts, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CloudIndexingJobDto job = await _apiClient.GetCloudIndexingJobAsync(jobId, cancellationToken);

                ApplyCloudJobProgress(job, accountName, accountIndex, totalAccounts);

                if (IsTerminalCloudJobStatus(job.Status))
                {
                    if (job.Status == CloudIndexingJobStatus.Failed)
                    {
                        string error = string.IsNullOrWhiteSpace(job.ErrorMessage)
                            ? "Cloud indexing failed."
                            : job.ErrorMessage;

                        throw new InvalidOperationException(error);
                    }

                    if (job.Status == CloudIndexingJobStatus.Cancelled)
                        throw new OperationCanceledException();

                    return;
                }

                await Task.Delay(500, cancellationToken);
            }
        }

        private void ApplyCloudJobProgress(CloudIndexingJobDto job, string accountName, int accountIndex, int totalAccounts)
        {
            RunOnUiThread(() =>
            {
                State.StorageName = $"Dropbox {accountIndex}/{totalAccounts}";
                State.IsIndeterminate = job.IsIndeterminate;

                State.TotalFiles = job.TotalFiles;
                State.ProcessedFiles = job.ProcessedFiles;
                State.FailedFiles = job.FailedFiles;

                State.CurrentFilePath =
                    !string.IsNullOrWhiteSpace(job.CurrentFilePath)
                        ? job.CurrentFilePath
                        : job.CurrentFileName;

                string status = string.IsNullOrWhiteSpace(job.StatusMessage)
                    ? job.Status.ToString()
                    : job.StatusMessage;

                State.StatusMessage = $"{accountName}: {status}";
            });
        }

        private bool IsTerminalCloudJobStatus(CloudIndexingJobStatus status)
        {
            return status is
                CloudIndexingJobStatus.Completed or
                CloudIndexingJobStatus.Failed or
                CloudIndexingJobStatus.Cancelled;
        }

        private void ApplyProgress(IndexingProgress progress)
        {
            State.TotalFiles = progress.TotalFiles;
            State.ProcessedFiles = progress.ProcessedFiles;
            State.FailedFiles = progress.FailedFiles;
            State.CurrentFilePath = progress.CurrentFilePath;
            State.IsIndeterminate = progress.IsIndeterminate;
            State.StatusMessage = progress.StatusMessage;
        }

        private void FinishIndexing()
        {
            RunOnUiThread(() =>
            {
                State.IsIndexing = false;
                State.IsIndeterminate = false;
                State.CurrentFilePath = null;
            });

            _currentCloudJobId = null;

            _currentCts?.Dispose();
            _currentCts = null;
        }

        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current.Dispatcher;

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }
    }
}