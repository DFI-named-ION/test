using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Services;
using FMVideoManagerApp.ViewModels.Items;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public sealed class FileListViewModel : ObservableObject, IComponentViewModel
    {
        private readonly MessageService _messageService;
        private readonly ApiClient _apiClient;
        private readonly TagService _tagService;
        private readonly FileIndexingService _fileIndexingService;
        private readonly HierarchyService _hierarchyService;

        public FileLibraryService Library { get; }
        public HierarchyService Hierarchy => _hierarchyService;
        public FileIndexingState Indexing => _fileIndexingService.State;

        private HierarchyItemViewModel? _selectedEntry;
        public HierarchyItemViewModel? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry != value)
                {
                    _selectedEntry = value;
                    OnPropertyChanged(nameof(SelectedEntry));

                    _ = RefreshSelectedItemTagsAsync();
                    _ = LoadSelectedFileReferencesAsync();
                }
            }
        }

        private int _columnsCount = 1;
        public int ColumnsCount
        {
            get => _columnsCount;
            set
            {
                if (_columnsCount != value)
                {
                    _columnsCount = value;
                    OnPropertyChanged(nameof(ColumnsCount));
                    OnPropertyChanged(nameof(CardHeight));
                    OnPropertyChanged(nameof(CardWidth));
                    OnPropertyChanged(nameof(ImageWidth));
                    OnPropertyChanged(nameof(ImageHeight));
                    OnPropertyChanged(nameof(CardFontSize));
                }
            }
        }

        public double CardHeight => ColumnsCount switch
        {
            1 => 270,
            2 => 230,
            3 => 190,
            4 => 150,
            5 => 125,
            6 => 100,
            _ => 100
        };

        public double CardWidth => ColumnsCount switch
        {
            1 => 555,
            2 => 267,
            3 => 171,
            4 => 123,
            5 => 95,
            6 => 75,
            _ => 75
        };

        public double ImageWidth => ColumnsCount switch
        {
            1 => 300,
            2 => 220,
            3 => 155,
            4 => 110,
            5 => 80,
            6 => 60,
            _ => 60
        };

        public double ImageHeight => ColumnsCount switch
        {
            1 => 200,
            2 => 150,
            3 => 115,
            4 => 85,
            5 => 65,
            6 => 50,
            _ => 50
        };

        public double CardFontSize => ColumnsCount switch
        {
            1 => 18,
            2 => 16,
            3 => 14,
            4 => 12,
            5 => 11,
            6 => 10,
            _ => 10
        };

        public ObservableCollection<HierarchyItemViewModel> Breadcrumbs => _hierarchyService.Breadcrumbs;

        public ObservableCollection<TagItemViewModel> SelectedItemTags => _tagService.SelectedNodeTags;

        public string SelectedItemTagsHeader => $"Tags ({SelectedItemTags.Count}):";

        private ObservableCollection<StorageReferenceItemViewModel> _selectedFileReferences = new();
        public ObservableCollection<StorageReferenceItemViewModel> SelectedFileReferences
        {
            get => _selectedFileReferences;
            private set
            {
                if (_selectedFileReferences != value)
                {
                    _selectedFileReferences = value;
                    OnPropertyChanged(nameof(SelectedFileReferences));
                    OnPropertyChanged(nameof(SelectedFileReferencesHeader));
                }
            }
        }

        public string SelectedFileReferencesHeader => $"References: ({SelectedFileReferences.Count})";

        public ICommand SelectEntryCommand { get; }
        public ICommand OpenHierarchyItemCommand { get; }
        public ICommand GoUpCommand { get; }
        public ICommand OpenRootCommand { get; }
        public ICommand OpenBreadcrumbCommand { get; }
        public ICommand DeleteSelectedNodeCommand { get; }
        public ICommand SaveSelectedNodeTitleCommand { get; }
        public ICommand SaveSelectedNodeDescriptionCommand { get; }
        public ICommand SaveSelectedNodeNotesCommand { get; }

        public ICommand StartLocalIndexingCommand { get; }
        public ICommand CancelLocalIndexingCommand { get; }
        public ICommand SyncPendingLocalFilesCommand { get; }
        public ICommand StartCloudIndexingCommand { get; }

        public FileListViewModel(MessageService messageService, ApiClient apiClient, FileIndexingService fileIndexingService, FileLibraryService library,
            HierarchyService hierarchyService, TagService tagService)
        {
            _messageService = messageService;
            _apiClient = apiClient;
            _fileIndexingService = fileIndexingService;
            Library = library;
            _hierarchyService = hierarchyService;
            _tagService = tagService;

            StartLocalIndexingCommand = new RelayCommand(async _ => await StartLocalIndexingAsync());
            CancelLocalIndexingCommand = new RelayCommand(_ => CancelLocalIndexing());
            SyncPendingLocalFilesCommand = new RelayCommand(async _ => await SyncPendingFilesAsync());
            StartCloudIndexingCommand = new RelayCommand(async _ => await StartCloudIndexingAsync());

            SelectEntryCommand = new RelayCommand(item =>
            {
                if (item is not HierarchyItemViewModel hierarchyItem)
                    return;

                SelectedEntry = hierarchyItem;
            });


            // HIERARCHY
            {
                OpenHierarchyItemCommand = new RelayCommand(entry =>
                {
                    if (entry is not HierarchyItemViewModel item)
                        return;

                    if (item.IsGroup)
                    {
                        _hierarchyService.OpenFolder(item.Id);
                        SelectedEntry = null;
                        return;
                    }

                    SelectedEntry = item;
                });
                GoUpCommand = new RelayCommand(_ =>
                {
                    _hierarchyService.GoUp();
                    SelectedEntry = null;
                });
                OpenRootCommand = new RelayCommand(_ =>
                {
                    _hierarchyService.OpenRoot();
                    SelectedEntry = null;
                });
                OpenBreadcrumbCommand = new RelayCommand(item =>
                {
                    if (item is not HierarchyItemViewModel hierarchyItem)
                        return;

                    _hierarchyService.OpenFolder(hierarchyItem.Id);
                });

                DeleteSelectedNodeCommand = new RelayCommand(async _ => await DeleteSelectedNodeAsync());
                SaveSelectedNodeTitleCommand = new RelayCommand(async _ => await SaveSelectedNodeTitleAsync());
                SaveSelectedNodeDescriptionCommand = new RelayCommand(async _ => await SaveSelectedNodeDescriptionAsync());
                SaveSelectedNodeNotesCommand = new RelayCommand(async _ => await SaveSelectedNodeNotesAsync());
            }

        }

        public void AssingColumnsCount(int count)
        {
            ColumnsCount = count;
        }

        private async Task LoadSelectedFileReferencesAsync()
        {
            try
            {
                if (SelectedEntry == null || !SelectedEntry.IsFile)
                {
                    SelectedFileReferences.Clear();
                    OnPropertyChanged(nameof(SelectedFileReferencesHeader));
                    return;
                }

                List<StorageReferenceDto> references = await _apiClient.GetFileReferencesAsync(SelectedEntry.Id);

                SelectedFileReferences = new ObservableCollection<StorageReferenceItemViewModel>(references.Select(x => new StorageReferenceItemViewModel(x)));
            }
            catch (Exception ex)
            {
                SelectedFileReferences.Clear();
                OnPropertyChanged(nameof(SelectedFileReferencesHeader));

                _messageService.ShowError($"Failed to load file references: {ex.Message}");
            }
        }

        private async Task StartLocalIndexingAsync()
        {
            try
            {
                _messageService.ShowMessage("Local indexing has started.");

                await _fileIndexingService.StartIndexingAsync();

                await RefreshAfterIndexingAsync();

                _messageService.ShowMessage("Local indexing finished.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError($"Error happened during local indexing: {ex.Message}");
            }
        }

        private void CancelLocalIndexing()
        {
            _fileIndexingService.CancelIndexing();
            _messageService.ShowMessage("Local indexing canceled.");
        }

        private async Task SyncPendingFilesAsync()
        {
            try
            {
                await _fileIndexingService.SyncPendingFilesAsync();

                await RefreshAfterIndexingAsync();

                _messageService.ShowMessage("Pending local sync finished.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError($"Error happened during local sync: {ex.Message}");
            }
        }

        private async Task StartCloudIndexingAsync() // move, fix
        {
            try
            {
                List<CloudProviderAccountDto> accounts = await _apiClient.GetCloudAccountsAsync();

                List<CloudProviderAccountDto> activeDropboxAccounts = accounts
                    .Where(x =>
                        x.Provider == CloudProviderType.Dropbox &&
                        x.IsActive)
                    .ToList();

                if (activeDropboxAccounts.Count == 0)
                {
                    _messageService.ShowWarning("No active Dropbox accounts connected.");
                    return;
                }

                foreach (CloudProviderAccountDto account in activeDropboxAccounts)
                {
                    _messageService.ShowMessage($"Cloud indexing for {account.DisplayName ?? account.Email} has started.");

                    await _apiClient.IndexDropboxAccountAsync(account.Id);
                }

                await RefreshAfterIndexingAsync();

                _messageService.ShowMessage("Cloud indexing finished.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError($"Error happened during cloud indexing: {ex.Message}");
            }
        }

        private async Task RefreshAfterIndexingAsync()
        {
            await Library.RefreshAsync();
            await _hierarchyService.RefreshAsync();
        }

        private async Task DeleteSelectedNodeAsync()
        {
            try
            {
                await _hierarchyService.DeleteNodeAsync(SelectedEntry.Id);

                _messageService.ShowMessage($"{(SelectedEntry.IsFile ? "File" : $"Group")} \"{SelectedEntry.Title}\" removed.");
                SelectedEntry = null;
            }
            catch (Exception ex)
            {
                _messageService.ShowError($"Error happened during group removal: {ex.Message}");
            }
        }

        private async Task SaveSelectedNodeTitleAsync()
        {
            if (SelectedEntry == null)
                return;

            try
            {
                await _hierarchyService.RenameNodeAsync(SelectedEntry.Id, SelectedEntry.EditableTitle);
                SelectedEntry = null;
                _messageService.ShowMessage("Title saved.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task SaveSelectedNodeDescriptionAsync()
        {
            if (SelectedEntry == null)
                return;

            try
            {
                await _hierarchyService.UpdateNodeDescriptionAsync(SelectedEntry.Id, SelectedEntry.EditableDescription);
                SelectedEntry = null;
                _messageService.ShowMessage("Description saved.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task SaveSelectedNodeNotesAsync()
        {
            if (SelectedEntry == null)
                return;

            try
            {
                await _hierarchyService.UpdateNodeNotesAsync(SelectedEntry.Id, SelectedEntry.EditableNotes);
                SelectedEntry = null;
                _messageService.ShowMessage("Notes saved.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task RefreshSelectedItemTagsAsync()
        {
            try
            {
                if (SelectedEntry == null)
                {
                    _tagService.ClearSelectedNodeTags();
                    OnPropertyChanged(nameof(SelectedItemTagsHeader));
                    return;
                }

                await _tagService.RefreshNodeTagsAsync(SelectedEntry.Id);

                OnPropertyChanged(nameof(SelectedItemTagsHeader));
            }
            catch (Exception ex)
            {
                _tagService.ClearSelectedNodeTags();
                OnPropertyChanged(nameof(SelectedItemTagsHeader));

                _messageService.ShowError($"Failed to load tags: {ex.Message}");
            }
        }
    }
}