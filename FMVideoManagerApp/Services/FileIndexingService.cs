using FFMpegCore;
using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Data.Repositories.LocalFileLocationRepository;
using FMVideoManagerApp.Models;
using System.IO;
using System.Windows;

namespace FMVideoManagerApp.Services
{
    public sealed class FileIndexingService
    {
        private readonly LocalDeviceService _localDeviceService;
        private readonly AuthService _authService;
        private readonly LocalIndexedPathService _indexedPathService;
        private readonly ILocalFileLocationRepository _fileLocationRepository;
        private readonly ApiClient _apiClient;

        private CancellationTokenSource? _indexingCts;

        public FileIndexingState State { get; } = new();

        public event Action<LocalFileLocation>? FileProcessed;
        public event Action<string, string>? FileProcessingFailed;

        public FileIndexingService(LocalDeviceService localDeviceService, AuthService authService, LocalIndexedPathService indexedPathService,
            ILocalFileLocationRepository fileLocationRepository, ApiClient apiClient)
        {
            _localDeviceService = localDeviceService;
            _authService = authService;
            _indexedPathService = indexedPathService;
            _fileLocationRepository = fileLocationRepository;
            _apiClient = apiClient;
        }

        public async Task StartIndexingAsync(CancellationToken cancellationToken = default)
        {
            if (State.IsIndexing)
                return;

            RunOnUiThread(() =>
            {
                State.Reset();
                State.IsIndexing = true;
                State.IsIndeterminate = true;
                State.StatusMessage = "Searching for files...";
            });

            long serverUserId = _authService.GetCurrentUserId();

            List<LocalIndexedPath> indexedPaths = _indexedPathService
                .GetCurrentUserPaths()
                .Where(x => x.IsEnabled)
                .ToList();

            if (indexedPaths.Count == 0)
            {
                RunOnUiThread(() =>
                {
                    State.IsIndexing = false;
                    State.IsIndeterminate = false;
                });

                throw new InvalidOperationException("No indexing paths configured.");
            }

            _indexingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken indexingToken = _indexingCts.Token;

            try
            {
                await Task.Run(
                    () => RunIndexingAsync(serverUserId, indexedPaths, indexingToken),
                    indexingToken);
            }
            catch (OperationCanceledException)
            {
                RunOnUiThread(() =>
                {
                    State.StatusMessage = "Indexing cancelled.";
                });
            }
            finally
            {
                RunOnUiThread(() =>
                {
                    State.IsIndexing = false;
                    State.IsIndeterminate = false;
                });

                _indexingCts.Dispose();
                _indexingCts = null;
            }
        }

        public void CancelIndexing()
        {
            _indexingCts?.Cancel();
        }

        private async Task RunIndexingAsync(long serverUserId, List<LocalIndexedPath> indexedPaths,
            CancellationToken cancellationToken)
        {
            SetIndexingStatus(0, 0, 0, null, true, "Searching for files...");

            List<FileInfo> files = CollectFiles(indexedPaths, cancellationToken);

            int total = files.Count;
            int processed = 0;
            int failed = 0;

            SetIndexingStatus(total, 0, 0, null, false, $"Found {total} files.");

            foreach (FileInfo file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    LocalIndexedPath? ownerPath = FindOwnerIndexedPath(indexedPaths, file.FullName);

                    if (ownerPath == null)
                    {
                        processed++;

                        SetIndexingStatus(total, processed, failed, file.FullName, false, $"Skipped {processed}/{total}");

                        continue;
                    }

                    SetIndexingStatus(total, processed, failed, file.FullName, false, $"Indexing {file.Name}...");

                    LocalFileLocation indexedFile = IndexFileLocally(serverUserId, ownerPath.Id, file);

                    SetIndexingStatus(total, processed, failed, file.FullName, false, $"Syncing {file.Name} with server...");

                    LocalFileLocation syncedFile = await TryRegisterLocalFileLocationOnServerAsync(indexedFile, cancellationToken);

                    processed++;

                    PublishProcessedFile(syncedFile);

                    SetIndexingStatus(total, processed, failed, file.FullName, false, $"Indexed {processed}/{total}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    processed++;
                    failed++;

                    PublishFailedFile(file.FullName, ex.Message);

                    SetIndexingStatus(
                        total,
                        processed,
                        failed,
                        file.FullName,
                        false,
                        $"Failed {failed} file(s). Indexed {processed}/{total}");
                }
            }

            SetIndexingCompleted(total, failed);
        }

        private static List<FileInfo> CollectFiles(List<LocalIndexedPath> indexedPaths, CancellationToken cancellationToken)
        {
            List<FileInfo> files = new();

            foreach (LocalIndexedPath indexedPath in indexedPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(indexedPath.Path))
                    continue;

                EnumerationOptions options = new()
                {
                    RecurseSubdirectories = indexedPath.IncludeSubdirectories,
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive
                };

                IEnumerable<string> filePaths = Directory.EnumerateFiles(indexedPath.Path, "*.mp4", options);

                foreach (string filePath in filePaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    files.Add(new FileInfo(filePath));
                }
            }

            return files;
        }

        private static LocalIndexedPath? FindOwnerIndexedPath(List<LocalIndexedPath> indexedPaths, string filePath)
        {
            string normalizedFilePath = Path.GetFullPath(filePath);

            return indexedPaths
                .OrderByDescending(x => x.Path.Length)
                .FirstOrDefault(x =>
                {
                    string normalizedIndexedPath = Path.GetFullPath(x.Path);

                    return normalizedFilePath.StartsWith(
                        normalizedIndexedPath,
                        StringComparison.OrdinalIgnoreCase);
                });
        }

        private LocalFileLocation IndexFileLocally(long serverUserId, long localIndexedPathId, FileInfo file)
        {
            string normalizedPath = Path.GetFullPath(file.FullName);
            DateTime lastModifiedUtc = file.LastWriteTimeUtc;

            LocalFileLocation? existing = _fileLocationRepository.FindByPath(serverUserId, normalizedPath);

            string? hash = existing?.ContentHash;

            bool needsHash =
                existing == null ||
                existing.SizeBytes != file.Length ||
                existing.LastModifiedUtc != lastModifiedUtc ||
                string.IsNullOrWhiteSpace(existing.ContentHash);

            if (needsHash)
            {
                hash = CryptographyService.HashFile(file);
            }

            bool sameKnownContent =
                existing != null &&
                !string.IsNullOrWhiteSpace(existing.ContentHash) &&
                existing.ContentHash == hash;

            long? serverFileItemId = sameKnownContent
                ? existing!.ServerFileItemId
                : null;

            var localFile = new LocalFileLocation
            {
                Id = existing?.Id ?? 0,
                ServerUserId = serverUserId,
                ServerFileItemId = serverFileItemId,
                ContentHash = hash,
                LocalIndexedPathId = localIndexedPathId,
                Path = normalizedPath,
                Filename = file.Name,
                SizeBytes = file.Length,
                LastModifiedUtc = lastModifiedUtc,
                LastSeenUtc = DateTime.UtcNow,
                ExistsOnDisk = true,

                SyncState = serverFileItemId == null
                    ? LocalFileSyncState.PendingSync
                    : LocalFileSyncState.Synced,

                LastSyncedAtUtc = serverFileItemId == null
                    ? null
                    : existing?.LastSyncedAtUtc,

                LastSyncError = null
            };

            return _fileLocationRepository.Upsert(localFile);
        }

        public async Task SyncPendingFilesAsync(IProgress<FileSyncProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            long serverUserId = _authService.GetCurrentUserId();

            List<LocalFileLocation> pendingFiles =
                _fileLocationRepository.GetPendingSyncFiles(serverUserId);

            int total = pendingFiles.Count;
            int processed = 0;

            progress?.Report(new FileSyncProgress
            {
                TotalFiles = total,
                ProcessedFiles = 0,
                StatusMessage = $"Found {total} files to sync."
            });

            foreach (LocalFileLocation file in pendingFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LocalFileLocation syncedFile = await TryRegisterLocalFileLocationOnServerAsync(
                    file,
                    cancellationToken);

                processed++;

                progress?.Report(new FileSyncProgress
                {
                    TotalFiles = total,
                    ProcessedFiles = processed,
                    SyncedFile = syncedFile,
                    StatusMessage = $"Synced {processed}/{total}"
                });
            }

            progress?.Report(new FileSyncProgress
            {
                TotalFiles = total,
                ProcessedFiles = total,
                IsCompleted = true,
                StatusMessage = "Sync completed."
            });
        }

        private async Task<LocalFileLocation> TryRegisterLocalFileLocationOnServerAsync(LocalFileLocation localFile,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(localFile.ContentHash))
            {
                return MarkLocalFileSyncFailed(localFile, "File hash is missing.");
            }

            try
            {
                RegisterLocalFileRequest request = CreateRegisterLocalFileRequest(localFile);

                RegisterLocalFileResponse response = await _apiClient.RegisterLocalFileAsync(request, cancellationToken);

                return MarkLocalFileSynced(localFile, response.ServerFileItemId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return MarkLocalFileSyncFailed(localFile, ex.Message);
            }
        }

        private RegisterLocalFileRequest CreateRegisterLocalFileRequest(LocalFileLocation localFile)
        {
            long? durationMs = null;
            int? width = null;
            int? height = null;

            try
            {
                if (File.Exists(localFile.Path))
                {
                    var mediaInfo = FFProbe.Analyse(localFile.Path);

                    durationMs = (long)mediaInfo.Duration.TotalMilliseconds;
                    width = mediaInfo.PrimaryVideoStream?.Width;
                    height = mediaInfo.PrimaryVideoStream?.Height;
                }
            }
            catch { }

            return new RegisterLocalFileRequest
            {
                ContentHash = localFile.ContentHash!,
                OriginalFilename = localFile.Filename,
                Title = localFile.Filename,
                SizeBytes = localFile.SizeBytes,
                ParentNodeId = null,
                Notes = null,

                DurationMs = durationMs,
                Width = width,
                Height = height,
                MimeType = "video/mp4",

                LocalPath = localFile.Path,
                LocalDeviceId = _localDeviceService.GetDeviceId()
            };
        }

        private LocalFileLocation MarkLocalFileSynced(LocalFileLocation localFile, long serverFileItemId)
        {
            _fileLocationRepository.MarkSynced(
                localFile.Id,
                localFile.ServerUserId,
                serverFileItemId);

            localFile.ServerFileItemId = serverFileItemId;
            localFile.SyncState = LocalFileSyncState.Synced;
            localFile.LastSyncedAtUtc = DateTime.UtcNow;
            localFile.LastSyncError = null;

            return localFile;
        }

        private LocalFileLocation MarkLocalFileSyncFailed(LocalFileLocation localFile, string error)
        {
            _fileLocationRepository.MarkSyncFailed(
                localFile.Id,
                localFile.ServerUserId,
                error);

            localFile.SyncState = LocalFileSyncState.SyncFailed;
            localFile.LastSyncError = error;
            localFile.LastSyncedAtUtc = null;

            return localFile;
        }

        private void SetIndexingStatus(int totalFiles, int processedFiles, int failedFiles, string? currentFilePath,
            bool isIndeterminate, string statusMessage)
        {
            PostToUiThread(() =>
            {
                State.TotalFiles = totalFiles;
                State.ProcessedFiles = processedFiles;
                State.FailedFiles = failedFiles;
                State.CurrentFilePath = currentFilePath;
                State.IsIndeterminate = isIndeterminate;
                State.StatusMessage = statusMessage;
            });
        }

        private void SetIndexingCompleted(int totalFiles, int failedFiles)
        {
            PostToUiThread(() =>
            {
                State.TotalFiles = totalFiles;
                State.ProcessedFiles = totalFiles;
                State.FailedFiles = failedFiles;
                State.CurrentFilePath = null;
                State.IsIndeterminate = false;
                State.StatusMessage = failedFiles == 0
                    ? "Indexing completed."
                    : $"Indexing completed with {failedFiles} failed file(s).";
            });
        }

        private void PublishProcessedFile(LocalFileLocation file)
        {
            PostToUiThread(() =>
            {
                FileProcessed?.Invoke(file);
            });
        }

        private void PublishFailedFile(string path, string error)
        {
            PostToUiThread(() =>
            {
                FileProcessingFailed?.Invoke(path, error);
            });
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

        private static void PostToUiThread(Action action)
        {
            var dispatcher = Application.Current.Dispatcher;

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(action);
            }
        }
    }
}