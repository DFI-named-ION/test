using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO.Tags;
using FMVideoManagerApp.Services;
using FMVideoManagerApp.ViewModels.Items;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public sealed class HierarchyRelationsViewModel : ObservableObject, IComponentViewModel
    {
        private readonly TagService _tagService;
        private readonly HierarchyService _hierarchyService;
        private readonly MessageService _messageService;

        public ObservableCollection<HierarchyParentOptionViewModel> ParentItemsLeft { get; } = new();
        public ObservableCollection<HierarchyParentOptionViewModel> ParentItemsRight { get; } = new();

        public ObservableCollection<HierarchyItemViewModel> ItemsLeft { get; } = new();
        public ObservableCollection<HierarchyItemViewModel> ItemsRight { get; } = new();

        private HierarchyParentOptionViewModel? _selectedParentItemLeft;
        public HierarchyParentOptionViewModel? SelectedParentItemLeft
        {
            get => _selectedParentItemLeft;
            set
            {
                if (_selectedParentItemLeft != value)
                {
                    _selectedParentItemLeft = value;
                    OnPropertyChanged(nameof(SelectedParentItemLeft));
                    RebuildLeftItems();
                }
            }
        }

        private HierarchyParentOptionViewModel? _selectedParentItemRight;
        public HierarchyParentOptionViewModel? SelectedParentItemRight
        {
            get => _selectedParentItemRight;
            set
            {
                if (_selectedParentItemRight != value)
                {
                    _selectedParentItemRight = value;
                    OnPropertyChanged(nameof(SelectedParentItemRight));
                    RebuildRightItems();
                }
            }
        }

        private HierarchyItemViewModel? _selectedItemLeft;
        public HierarchyItemViewModel? SelectedItemLeft
        {
            get => _selectedItemLeft;
            set
            {
                if (_selectedItemLeft != value)
                {
                    _selectedItemLeft = value;
                    OnPropertyChanged(nameof(SelectedItemLeft));

                    if (value != null)
                        SelectedItemRight = null;

                    OnPropertyChanged(nameof(SelectedItem));
                    _ = RefreshSelectedItemTagsAsync();
                }
            }
        }

        private HierarchyItemViewModel? _selectedItemRight;
        public HierarchyItemViewModel? SelectedItemRight
        {
            get => _selectedItemRight;
            set
            {
                if (_selectedItemRight != value)
                {
                    _selectedItemRight = value;
                    OnPropertyChanged(nameof(SelectedItemRight));

                    if (value != null)
                        SelectedItemLeft = null;

                    OnPropertyChanged(nameof(SelectedItem));
                    _ = RefreshSelectedItemTagsAsync();
                }
            }
        }

        public HierarchyItemViewModel? SelectedItem => SelectedItemLeft ?? SelectedItemRight;

        private string _newGroupTitle = "New group";
        public string NewGroupTitle
        {
            get => _newGroupTitle;
            set
            {
                if (_newGroupTitle != value)
                {
                    _newGroupTitle = value;
                    OnPropertyChanged(nameof(NewGroupTitle));
                }
            }
        }


        public ObservableCollection<TagItemViewModel> Tags => _tagService.Tags;
        public ICollectionView TagsView { get; }

        private string _tagSearchText = string.Empty;
        public string TagSearchText
        {
            get => _tagSearchText;
            set
            {
                if (_tagSearchText != value)
                {
                    _tagSearchText = value;
                    OnPropertyChanged(nameof(TagSearchText));
                    TagsView.Refresh();
                }
            }
        }

        public ObservableCollection<TagItemViewModel> SelectedItemTags => _tagService.SelectedNodeTags;

        private TagItemViewModel? _selectedTag;
        public TagItemViewModel? SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (_selectedTag != value)
                {
                    _selectedTag = value;
                    OnPropertyChanged(nameof(SelectedTag));
                }
            }
        }

        private TagItemViewModel? _selectedItemTag;
        public TagItemViewModel? SelectedItemTag
        {
            get => _selectedItemTag;
            set
            {
                if (_selectedItemTag != value)
                {
                    _selectedItemTag = value;
                    OnPropertyChanged(nameof(SelectedItemTag));
                }
            }
        }

        private string _newTagName = "New tag";
        public string NewTagName
        {
            get => _newTagName;
            set
            {
                if (_newTagName != value)
                {
                    _newTagName = value;
                    OnPropertyChanged(nameof(NewTagName));
                }
            }
        }


        public ICommand MoveRightCommand { get; }
        public ICommand MoveLeftCommand { get; }
        public ICommand CreateGroupLeftCommand { get; }
        public ICommand CreateGroupRightCommand { get; }
        public ICommand OpenLeftParentFromItemCommand { get; }
        public ICommand OpenRightParentFromItemCommand { get; }

        public ICommand CreateTagCommand { get; }
        public ICommand ApplyTagCommand { get; }
        public ICommand RemoveTagCommand { get; }

        public HierarchyRelationsViewModel(HierarchyService hierarchyService, MessageService messageService, TagService tagService)
        {
            _hierarchyService = hierarchyService;
            _messageService = messageService;
            _tagService = tagService;

            TagsView = CollectionViewSource.GetDefaultView(_tagService.Tags);
            TagsView.Filter = FilterTag;

            MoveRightCommand = new RelayCommand(async _ => await MoveRightAsync());
            MoveLeftCommand = new RelayCommand(async _ => await MoveLeftAsync());
            CreateGroupLeftCommand = new RelayCommand(async _ => await CreateGroupLeftAsync());
            CreateGroupRightCommand = new RelayCommand(async _ => await CreateGroupRightAsync());

            OpenLeftParentFromItemCommand = new RelayCommand(item =>
            {
                if (item is HierarchyItemViewModel hierarchyItem)
                    OpenLeftParentFromItem(hierarchyItem);
            });
            OpenRightParentFromItemCommand = new RelayCommand(item =>
            {
                if (item is HierarchyItemViewModel hierarchyItem)
                    OpenRightParentFromItem(hierarchyItem);
            });

            CreateTagCommand = new RelayCommand(async _ => await CreateTagAsync());
            ApplyTagCommand = new RelayCommand(async _ => await ApplyTagAsync());
            RemoveTagCommand = new RelayCommand(async _ => await RemoveTagAsync());
        }

        public async Task OnActivatedAsync()
        {
            await RefreshAsync();
        }

        public void OnDeactivated()
        {
            SelectedItemLeft = null;
            SelectedItemRight = null;
        }

        private void OpenLeftParentFromItem(HierarchyItemViewModel item)
        {
            if (!item.IsGroup)
                return;

            HierarchyParentOptionViewModel? parent = ParentItemsLeft
                .FirstOrDefault(x => x.NodeId == item.Id);

            if (parent == null)
                return;

            SelectedParentItemLeft = parent;
        }

        private void OpenRightParentFromItem(HierarchyItemViewModel item)
        {
            if (!item.IsGroup)
                return;

            HierarchyParentOptionViewModel? parent = ParentItemsRight
                .FirstOrDefault(x => x.NodeId == item.Id);

            if (parent == null)
                return;

            SelectedParentItemRight = parent;
        }

        private async Task RefreshAsync()
        {
            try
            {
                await _hierarchyService.RefreshAsync();
                await _tagService.RefreshTagsAsync();
                TagsView.Refresh();

                RebuildParentLists();

                SelectedParentItemLeft = ParentItemsLeft.FirstOrDefault();
                SelectedParentItemRight = ParentItemsRight.FirstOrDefault();

                RebuildLeftItems();
                RebuildRightItems();

                await RefreshSelectedItemTagsAsync();
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
                SelectedItemTag = null;

                if (SelectedItem == null)
                {
                    _tagService.ClearSelectedNodeTags();
                    return;
                }

                await _tagService.RefreshNodeTagsAsync(SelectedItem.Id);
            }
            catch (Exception ex)
            {
                _messageService.ShowError($"Failed to load item tags: {ex.Message}");
            }
        }

        private void RebuildParentLists()
        {
            List<HierarchyParentOptionViewModel> parents = _hierarchyService.GetParentOptions();

            ParentItemsLeft.Clear();
            ParentItemsRight.Clear();

            foreach (HierarchyParentOptionViewModel parent in parents)
            {
                ParentItemsLeft.Add(parent);
                ParentItemsRight.Add(parent);
            }
        }

        private void RebuildLeftItems()
        {
            ItemsLeft.Clear();

            long? parentId = SelectedParentItemLeft?.NodeId;

            foreach (HierarchyItemViewModel item in _hierarchyService.GetItemsByParentId(parentId))
            {
                ItemsLeft.Add(item);
            }
        }

        private void RebuildRightItems()
        {
            ItemsRight.Clear();

            long? parentId = SelectedParentItemRight?.NodeId;

            foreach (HierarchyItemViewModel item in _hierarchyService.GetItemsByParentId(parentId))
            {
                ItemsRight.Add(item);
            }
        }

        private async Task MoveRightAsync()
        {
            if (SelectedItemLeft == null)
                return;

            if (SelectedParentItemRight == null)
                return;

            await MoveAsync(SelectedItemLeft, SelectedParentItemRight.NodeId);
        }

        private async Task MoveLeftAsync()
        {
            if (SelectedItemRight == null)
                return;

            if (SelectedParentItemLeft == null)
                return;

            await MoveAsync(SelectedItemRight, SelectedParentItemLeft.NodeId);
        }

        private async Task MoveAsync(HierarchyItemViewModel item, long? targetParentNodeId)
        {
            try
            {
                if (item.Id == targetParentNodeId)
                {
                    _messageService.ShowWarning("Cannot move item into itself.");
                    return;
                }

                if (item.ParentNodeId == targetParentNodeId)
                {
                    _messageService.ShowWarning("Item is already in that location.");
                    return;
                }

                long? selectedLeftParentId = SelectedParentItemLeft?.NodeId;
                long? selectedRightParentId = SelectedParentItemRight?.NodeId;

                await _hierarchyService.MoveNodeAsync(item.Id, targetParentNodeId);

                RebuildParentLists();

                SelectedParentItemLeft = ParentItemsLeft.FirstOrDefault(x => x.NodeId == selectedLeftParentId);
                SelectedParentItemRight = ParentItemsRight.FirstOrDefault(x => x.NodeId == selectedRightParentId);

                RebuildLeftItems();
                RebuildRightItems();

                SelectedItemLeft = null;
                SelectedItemRight = null;

                _messageService.ShowMessage("Item moved.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task CreateGroupLeftAsync()
        {
            await CreateGroupAsync(SelectedParentItemLeft);
        }

        private async Task CreateGroupRightAsync()
        {
            await CreateGroupAsync(SelectedParentItemRight);
        }

        private async Task CreateGroupAsync(HierarchyParentOptionViewModel? parent)
        {
            if (parent == null)
                return;

            if (string.IsNullOrWhiteSpace(NewGroupTitle))
            {
                _messageService.ShowWarning("Group title is required.");
                return;
            }

            try
            {
                long? selectedLeftParentId = SelectedParentItemLeft?.NodeId;
                long? selectedRightParentId = SelectedParentItemRight?.NodeId;

                await _hierarchyService.CreateGroupAsync(NewGroupTitle.Trim(), parent.NodeId);

                RebuildParentLists();

                SelectedParentItemLeft = ParentItemsLeft
                    .FirstOrDefault(x => x.NodeId == selectedLeftParentId);

                SelectedParentItemRight = ParentItemsRight
                    .FirstOrDefault(x => x.NodeId == selectedRightParentId);

                RebuildLeftItems();
                RebuildRightItems();

                _messageService.ShowMessage("Group created.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private bool FilterTag(object obj)
        {
            if (obj is not TagItemViewModel tag)
                return false;

            if (string.IsNullOrWhiteSpace(TagSearchText))
                return true;

            return tag.Name.Contains(
                TagSearchText.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task CreateTagAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTagName))
            {
                _messageService.ShowWarning("Tag name is required.");
                return;
            }

            try
            {
                TagItemViewModel created = await _tagService.CreateTagAsync(NewTagName);

                TagsView.Refresh();

                SelectedTag = created;
                NewTagName = string.Empty;

                _messageService.ShowMessage("Tag created.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task ApplyTagAsync()
        {
            if (SelectedItem == null)
            {
                _messageService.ShowWarning("Select item first.");
                return;
            }

            if (SelectedTag == null)
            {
                _messageService.ShowWarning("Select tag first.");
                return;
            }

            try
            {
                await _tagService.ApplyTagToNodeAsync(SelectedItem.Id, SelectedTag.Id);

                _messageService.ShowMessage("Tag applied.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task RemoveTagAsync()
        {
            if (SelectedItem == null)
            {
                _messageService.ShowWarning("Select item first.");
                return;
            }

            if (SelectedItemTag == null)
            {
                _messageService.ShowWarning("Select applied tag first.");
                return;
            }

            try
            {
                await _tagService.RemoveTagFromNodeAsync(
                    SelectedItem.Id,
                    SelectedItemTag.Id);

                SelectedItemTag = null;

                _messageService.ShowMessage("Tag removed.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }
    }
}