using FFMpegCore;
using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Data.DTO.Indexing;
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

        public async Task StartIndexingAsync(IProgress<IndexingProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            long serverUserId = _authService.GetCurrentUserId();

            List<LocalIndexedPath> indexedPaths = _indexedPathService
                .GetCurrentUserPaths()
                .Where(x => x.IsEnabled)
                .ToList();

            if (indexedPaths.Count == 0)
                throw new InvalidOperationException("No indexing paths configured.");

            await Task.Run(
                async () => await RunIndexingAsync(
                    serverUserId,
                    indexedPaths,
                    progress,
                    cancellationToken),
                cancellationToken);
        }


        private async Task RunIndexingAsync(long serverUserId, List<LocalIndexedPath> indexedPaths, IProgress<IndexingProgress>? progress,
            CancellationToken cancellationToken)
        {
            progress?.Report(new IndexingProgress
            {
                TotalFiles = 0,
                ProcessedFiles = 0,
                FailedFiles = 0,
                CurrentFilePath = null,
                IsIndeterminate = true,
                StatusMessage = "Searching for files..."
            });

            List<FileInfo> files = CollectFiles(indexedPaths, cancellationToken);

            int total = files.Count;
            int processed = 0;
            int failed = 0;

            progress?.Report(new IndexingProgress
            {
                TotalFiles = total,
                ProcessedFiles = 0,
                FailedFiles = 0,
                CurrentFilePath = null,
                IsIndeterminate = false,
                StatusMessage = $"Found {total} files."
            });

            foreach (FileInfo file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    LocalIndexedPath? ownerPath = FindOwnerIndexedPath(indexedPaths, file.FullName);

                    if (ownerPath == null)
                    {
                        processed++;

                        progress?.Report(new IndexingProgress
                        {
                            TotalFiles = total,
                            ProcessedFiles = processed,
                            FailedFiles = failed,
                            CurrentFilePath = file.FullName,
                            IsIndeterminate = false,
                            StatusMessage = $"Skipped {processed}/{total}"
                        });

                        continue;
                    }

                    progress?.Report(new IndexingProgress
                    {
                        TotalFiles = total,
                        ProcessedFiles = processed,
                        FailedFiles = failed,
                        CurrentFilePath = file.FullName,
                        IsIndeterminate = false,
                        StatusMessage = $"Indexing {file.Name}..."
                    });

                    LocalFileLocation indexedFile = IndexFileLocally(serverUserId, ownerPath.Id, file);

                    progress?.Report(new IndexingProgress
                    {
                        TotalFiles = total,
                        ProcessedFiles = processed,
                        FailedFiles = failed,
                        CurrentFilePath = file.FullName,
                        IsIndeterminate = false,
                        StatusMessage = $"Syncing {file.Name} with server..."
                    });

                    LocalFileLocation syncedFile = await TryRegisterLocalFileLocationOnServerAsync(indexedFile, cancellationToken);

                    processed++;

                    PublishProcessedFile(syncedFile);

                    progress?.Report(new IndexingProgress
                    {
                        TotalFiles = total,
                        ProcessedFiles = processed,
                        FailedFiles = failed,
                        CurrentFilePath = file.FullName,
                        IsIndeterminate = false,
                        StatusMessage = $"Indexed {processed}/{total}"
                    });
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

                    progress?.Report(new IndexingProgress
                    {
                        TotalFiles = total,
                        ProcessedFiles = processed,
                        FailedFiles = failed,
                        CurrentFilePath = file.FullName,
                        IsIndeterminate = false,
                        StatusMessage = $"Failed {failed} file(s). Indexed {processed}/{total}"
                    });
                }
            }

            progress?.Report(new IndexingProgress
            {
                TotalFiles = total,
                ProcessedFiles = total,
                FailedFiles = failed,
                CurrentFilePath = null,
                IsIndeterminate = false,
                StatusMessage = failed == 0
                    ? "Local indexing completed."
                    : $"Local indexing completed with {failed} failed file(s)."
            });
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

                LocalFileLocation syncedFile = await TryRegisterLocalFileLocationOnServerAsync(file, cancellationToken);

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

        private async Task<LocalFileLocation> TryRegisterLocalFileLocationOnServerAsync(LocalFileLocation localFile, CancellationToken cancellationToken)
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
            _fileLocationRepository.MarkSyncFailed(localFile.Id, localFile.ServerUserId, error);

            localFile.SyncState = LocalFileSyncState.SyncFailed;
            localFile.LastSyncError = error;
            localFile.LastSyncedAtUtc = null;

            return localFile;
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