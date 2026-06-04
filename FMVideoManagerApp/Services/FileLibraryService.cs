using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Data.Repositories.LocalFileLocationRepository;
using FMVideoManagerApp.Models;
using FMVideoManagerApp.ViewModels.Items;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;

namespace FMVideoManagerApp.Services
{
    public sealed class FileLibraryService
    {
        private readonly AuthService _authService;
        private readonly ILocalFileLocationRepository _localFileRepo;
        private readonly ApiClient _apiClient;
        private readonly FileIndexingService _fileIndexingService;

        public ObservableCollection<FileLibraryItemViewModel> AllFiles { get; } = new();

        public ICollectionView LocalOnlyFiles { get; private set; }
        public ICollectionView PendingSyncFiles { get; private set; }
        public ICollectionView SyncedFiles { get; private set; }
        public ICollectionView SyncFailedFiles { get; private set; }
        public ICollectionView RemoteOnlyFiles { get; private set; }
        public ICollectionView AvailableLocallyFiles { get; private set; }

        public event Action? CollectionsUpdated; // remove

        public FileLibraryService(AuthService authService, ILocalFileLocationRepository localFileRepo, ApiClient apiClient, FileIndexingService fileIndexingService)
        {
            _authService = authService;
            _localFileRepo = localFileRepo;
            _apiClient = apiClient;
            _fileIndexingService = fileIndexingService;

            LocalOnlyFiles = CreateView(IsLocalOnly);
            PendingSyncFiles = CreateView(IsPendingSync);
            SyncedFiles = CreateView(IsSynced);
            SyncFailedFiles = CreateView(IsSyncFailed);
            RemoteOnlyFiles = CreateView(IsRemoteOnly);
            AvailableLocallyFiles = CreateView(IsAvailableLocally);

            _authService.LoggedIn += async _ => await RefreshAsync();
            _authService.LoggedOut += Clear;

            _fileIndexingService.FileProcessed += AddOrUpdateLocalFile;
            _fileIndexingService.FileProcessingFailed += AddLocalIndexingFailure;
        }

        public async Task RefreshAsync()
        {
            if (!_authService.IsLoggedIn)
            {
                Clear();
                return;
            }

            long userId = _authService.GetCurrentUserId();

            List<LocalFileLocation> localFiles = _localFileRepo.GetByUserId(userId);

            List<ServerFileDto> serverFiles = await _apiClient.GetFilesAsync();

            RebuildCollections(localFiles, serverFiles);

            await Task.CompletedTask;
        }

        public void Clear()
        {
            AllFiles.Clear();
        }

        public void AddOrUpdateLocalFile(LocalFileLocation file)
        {
            FileLibraryItemViewModel newItem = CreateFromLocalFile(file);

            FileLibraryItemViewModel? existing = AllFiles
                .FirstOrDefault(x =>
                    x.LocalFileLocationId == file.Id ||
                    (
                        file.ServerFileItemId != null &&
                        x.ServerFileItemId == file.ServerFileItemId
                    ));

            if (existing == null)
            {
                AllFiles.Add(newItem);
            }
            else
            {
                int index = AllFiles.IndexOf(existing);

                if (index >= 0)
                {
                    AllFiles[index] = newItem;
                }
            }

            CollectionsUpdated?.Invoke();
        }

        public void AddLocalIndexingFailure(string path, string error)
        {
            FileLibraryItemViewModel? existing = AllFiles
                    .FirstOrDefault(x => x.LocalPath == path);

            if (existing != null)
            {
                AllFiles.Remove(existing);
            }

            AllFiles.Add(new FileLibraryItemViewModel
            {
                LocalPath = path,
                Title = Path.GetFileName(path),
                OriginalFilename = Path.GetFileName(path),
                IsIndexingFailed = true,
                IndexingError = error
            });

            CollectionsUpdated?.Invoke();
        }

        private void RebuildCollections(List<LocalFileLocation> localFiles, List<ServerFileDto> serverFiles)
        {
            AllFiles.Clear();

            Dictionary<long, LocalFileLocation> localByServerFileId = localFiles
                .Where(x => x.ServerFileItemId != null)
                .GroupBy(x => x.ServerFileItemId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            HashSet<long> usedLocalIds = new();

            foreach (ServerFileDto serverFile in serverFiles)
            {
                localByServerFileId.TryGetValue(
                    serverFile.ServerFileItemId,
                    out LocalFileLocation? localFile);

                if (localFile != null)
                    usedLocalIds.Add(localFile.Id);

                AllFiles.Add(CreateFromServerAndLocal(serverFile, localFile));

                CollectionsUpdated?.Invoke();
            }

            foreach (LocalFileLocation localFile in localFiles)
            {
                if (usedLocalIds.Contains(localFile.Id))
                    continue;

                AllFiles.Add(CreateFromLocalFile(localFile));

                CollectionsUpdated?.Invoke();
            }
        }

        private ICollectionView CreateView(Predicate<FileLibraryItemViewModel> filter)
        {
            var source = new CollectionViewSource
            {
                Source = AllFiles
            };

            source.Filter += (_, e) =>
            {
                if (e.Item is FileLibraryItemViewModel item)
                {
                    e.Accepted = filter(item);
                }
                else
                {
                    e.Accepted = false;
                }
            };

            return source.View;
        }

        private static bool IsLocalOnly(FileLibraryItemViewModel item) => item.IsAvailableLocally && !item.IsKnownByServer;

        private static bool IsPendingSync(FileLibraryItemViewModel item) => item.LocalSyncState == LocalFileSyncState.PendingSync;

        private static bool IsSynced(FileLibraryItemViewModel item) => item.LocalSyncState == LocalFileSyncState.Synced;

        private static bool IsSyncFailed(FileLibraryItemViewModel item) => item.LocalSyncState == LocalFileSyncState.SyncFailed;

        private static bool IsRemoteOnly(FileLibraryItemViewModel item) => !item.IsAvailableLocally && item.IsKnownByServer;

        private static bool IsAvailableLocally(FileLibraryItemViewModel item) => item.IsAvailableLocally;

        private static FileLibraryItemViewModel CreateFromLocalFile(LocalFileLocation file)
        {
            return new FileLibraryItemViewModel
            {
                LocalFileLocationId = file.Id,
                ServerFileItemId = file.ServerFileItemId,
                ServerNodeId = file.ServerFileItemId,
                ContentHash = file.ContentHash,
                Title = file.Filename,
                LocalPath = file.Path,
                OriginalFilename = file.Filename,
                SizeBytes = file.SizeBytes,
                LocalSyncState = file.SyncState
            };
        }

        private static FileLibraryItemViewModel CreateFromServerAndLocal(ServerFileDto serverFile, LocalFileLocation? localFile)
        {
            return new FileLibraryItemViewModel
            {
                LocalFileLocationId = localFile?.Id,
                ServerFileItemId = serverFile.ServerFileItemId,
                ServerNodeId = serverFile.NodeId,
                ContentHash = serverFile.ContentHash,
                Title = serverFile.Title,
                LocalPath = localFile?.Path,
                OriginalFilename = serverFile.OriginalFilename,
                SizeBytes = serverFile.SizeBytes,
                LocalSyncState = localFile?.SyncState,
                Width = serverFile.Width,
                Height = serverFile.Height,
                DurationMs = serverFile.DurationMs,
                Notes = serverFile.Notes,
                ParentNodeId = serverFile.ParentNodeId,
                // PreviewPath = localFile.PreviewPath
            };
        }
    }
}