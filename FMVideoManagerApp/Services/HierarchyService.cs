using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO.Hierarchy;
using FMVideoManagerApp.ViewModels.Items;
using System.Collections.ObjectModel;

namespace FMVideoManagerApp.Services
{
    public sealed class HierarchyService : ObservableObject
    {
        private readonly ApiClient _apiClient;
        private readonly AuthService _authService;

        private List<HierarchyNodeDto> _allNodes = new();
        public ObservableCollection<HierarchyItemViewModel> CurrentItems { get; } = new();
        public ObservableCollection<HierarchyItemViewModel> AllGroups { get; } = new();
        public string AllGroupsHeader => $"All Groups: ({AllGroups.Count})";

        public ObservableCollection<HierarchyItemViewModel> AllFiles { get; } = new();
        public string AllFilesHeader => $"All files: ({AllFiles.Count})";

        public ObservableCollection<HierarchyItemViewModel> Breadcrumbs { get; } = new();

        private long? _currentFolderId;
        public long? CurrentFolderId
        {
            get => _currentFolderId;
            private set
            {
                if (_currentFolderId != value)
                {
                    _currentFolderId = value;
                    OnPropertyChanged(nameof(CurrentFolderId));
                    OnPropertyChanged(nameof(IsAtRoot));
                }
            }
        }

        public bool IsAtRoot => CurrentFolderId == null;

        public HierarchyService(ApiClient apiClient, AuthService authService)
        {
            _apiClient = apiClient;
            _authService = authService;

            _authService.LoggedIn += async _ => await RefreshAsync();
            _authService.LoggedOut += Clear;
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (!_authService.IsLoggedIn)
            {
                Clear();
                return;
            }

            _allNodes = await _apiClient.GetHierarchyAsync(cancellationToken);

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public void OpenRoot()
        {
            CurrentFolderId = null;

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public void OpenFolder(long folderNodeId)
        {
            HierarchyNodeDto? folder = _allNodes.FirstOrDefault(x =>
                x.Id == folderNodeId &&
                x.NodeType == HierarchyNodeType.Group);

            if (folder == null)
                return;

            CurrentFolderId = folder.Id;

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public void GoUp()
        {
            if (CurrentFolderId == null)
                return;

            HierarchyNodeDto? current = _allNodes.FirstOrDefault(x => x.Id == CurrentFolderId.Value);

            CurrentFolderId = current?.ParentNodeId;

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public async Task<HierarchyItemViewModel> CreateGroupAsync(string title, long? parentNodeId = null, CancellationToken cancellationToken = default)
        {
            HierarchyNodeDto created = await _apiClient.CreateGroupAsync(title, parentNodeId, null, cancellationToken);

            _allNodes.Add(created);

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();

            return new HierarchyItemViewModel(created);
        }

        public async Task RenameNodeAsync(long nodeId, string newTitle, CancellationToken cancellationToken = default)
        {
            await _apiClient.RenameNodeAsync(nodeId, newTitle, cancellationToken);

            HierarchyNodeDto? node = _allNodes.FirstOrDefault(x => x.Id == nodeId);

            if (node != null)
            {
                node.Title = newTitle;
                node.UpdatedAtUtc = DateTime.UtcNow;
            }

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public async Task UpdateNodeDescriptionAsync(long nodeId, string? description, CancellationToken cancellationToken = default)
        {
            await _apiClient.UpdateNodeDescriptionAsync(nodeId, description, cancellationToken);

            HierarchyNodeDto? node = _allNodes.FirstOrDefault(x => x.Id == nodeId);

            if (node?.Group != null)
            {
                node.Group.Description = string.IsNullOrWhiteSpace(description)
                    ? null
                    : description.Trim();

                node.UpdatedAtUtc = DateTime.UtcNow;
            }

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public async Task UpdateNodeNotesAsync(long nodeId, string? notes, CancellationToken cancellationToken = default)
        {
            await _apiClient.UpdateNodeNotesAsync(nodeId, notes, cancellationToken);

            HierarchyNodeDto? node = _allNodes.FirstOrDefault(x => x.Id == nodeId);

            if (node?.File != null)
            {
                node.File.Notes = string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim();

                node.UpdatedAtUtc = DateTime.UtcNow;
            }

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public async Task MoveNodeAsync(long nodeId, long? newParentNodeId, int? sortOrder = null, CancellationToken cancellationToken = default)
        {
            await _apiClient.MoveNodeAsync(nodeId, newParentNodeId, sortOrder, cancellationToken);

            HierarchyNodeDto? node = _allNodes.FirstOrDefault(x => x.Id == nodeId);

            if (node != null)
            {
                node.ParentNodeId = newParentNodeId;

                if (sortOrder != null)
                    node.SortOrder = sortOrder.Value;

                node.UpdatedAtUtc = DateTime.UtcNow;
            }

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public async Task CopyNodeAsync(long nodeId, long? targetParentNodeId, int? sortOrder = null, CancellationToken cancellationToken = default)
        {
            await _apiClient.CopyNodeAsync(nodeId, targetParentNodeId, sortOrder, cancellationToken);
            await RefreshAsync(cancellationToken);
        }

        public async Task DeleteNodeAsync(long nodeId, CancellationToken cancellationToken = default)
        {
            await _apiClient.DeleteNodeAsync(nodeId, cancellationToken);

            _allNodes.RemoveAll(x => x.Id == nodeId);

            if (CurrentFolderId == nodeId)
                CurrentFolderId = null;

            RebuildAllItems();
            RebuildCurrentItems();
            RebuildBreadcrumbs();
        }

        public List<HierarchyParentOptionViewModel> GetParentOptions()
        {
            List<HierarchyParentOptionViewModel> result = new()
            {
                new HierarchyParentOptionViewModel(null, "Root")
            };

            result.AddRange(
                _allNodes
                    .Where(x => x.NodeType == HierarchyNodeType.Group)
                    .OrderBy(x => x.Title)
                    .Select(x => new HierarchyParentOptionViewModel(x.Id, x.Title)));

            return result;
        }

        public List<HierarchyItemViewModel> GetItemsByParentId(long? parentNodeId)
        {
            return _allNodes
                .Where(x => x.ParentNodeId == parentNodeId)
                .OrderBy(x => x.NodeType == HierarchyNodeType.File)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .Select(x => new HierarchyItemViewModel(x))
                .ToList();
        }

        private void Clear()
        {
            _allNodes.Clear();
            CurrentItems.Clear();
            Breadcrumbs.Clear();
            CurrentFolderId = null;
        }

        private void RebuildCurrentItems()
        {
            CurrentItems.Clear();

            List<HierarchyNodeDto> children = _allNodes
                .Where(x => x.ParentNodeId == CurrentFolderId)
                .OrderBy(x => x.NodeType == HierarchyNodeType.File)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToList();

            foreach (HierarchyNodeDto child in children)
            {
                CurrentItems.Add(new HierarchyItemViewModel(child));
            }
        }

        private void RebuildAllItems()
        {
            AllGroups.Clear();
            AllFiles.Clear();

            foreach (HierarchyNodeDto node in _allNodes
                .OrderBy(x => x.NodeType == HierarchyNodeType.File)
                .ThenBy(x => x.Title))
            {
                var item = new HierarchyItemViewModel(node);

                if (item.IsGroup)
                    AllGroups.Add(item);
                else if (item.IsFile)
                    AllFiles.Add(item);
            }

            OnPropertyChanged(nameof(AllGroupsHeader));
            OnPropertyChanged(nameof(AllFilesHeader));
        }

        private void RebuildBreadcrumbs()
        {
            Breadcrumbs.Clear();

            if (CurrentFolderId == null)
                return;

            List<HierarchyNodeDto> chain = new();

            HierarchyNodeDto? current = _allNodes.FirstOrDefault(x => x.Id == CurrentFolderId.Value);

            while (current != null)
            {
                chain.Add(current);

                if (current.ParentNodeId == null)
                    break;

                current = _allNodes.FirstOrDefault(x => x.Id == current.ParentNodeId.Value);
            }

            chain.Reverse();

            foreach (HierarchyNodeDto node in chain)
            {
                Breadcrumbs.Add(new HierarchyItemViewModel(node));
            }
        }
    }
}