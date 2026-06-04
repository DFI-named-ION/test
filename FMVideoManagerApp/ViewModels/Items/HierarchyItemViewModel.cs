using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO.Hierarchy;

namespace FMVideoManagerApp.ViewModels.Items
{
    public sealed class HierarchyItemViewModel : ObservableObject
    {
        public long Id { get; }

        public long? ParentNodeId { get; }

        public HierarchyNodeType NodeType { get; }

        public string Title { get; }

        public int SortOrder { get; }

        public FileNodeDto? File { get; }

        public GroupNodeDto? Group { get; }

        public bool IsGroup => NodeType == HierarchyNodeType.Group;

        public bool IsFile => NodeType == HierarchyNodeType.File;

        public string? ContentHash => File?.ContentHash;

        public long? SizeBytes => File?.SizeBytes;

        public string SizeText => SizeBytes == null ? string.Empty : SizeConverter.ToHumanReadable(SizeBytes.Value);

        public string Resolution =>
            File?.Width == null || File?.Height == null
                ? string.Empty
                : $"{File.Width}x{File.Height}";

        public string Duration
        {
            get
            {
                if (File?.DurationMs == null)
                    return string.Empty;

                return TimeSpan.FromMilliseconds(File.DurationMs.Value).ToString(@"hh\:mm\:ss\.fff");
            }
        }

        private string _editableTitle;
        public string EditableTitle
        {
            get => _editableTitle;
            set
            {
                if (_editableTitle != value)
                {
                    _editableTitle = value;
                    OnPropertyChanged(nameof(EditableTitle));
                }
            }
        }

        private string _editableDescription;
        public string EditableDescription
        {
            get => _editableDescription;
            set
            {
                if (_editableDescription != value)
                {
                    _editableDescription = value;
                    OnPropertyChanged(nameof(EditableDescription));
                }
            }
        }

        private string _editableNotes;
        public string EditableNotes
        {
            get => _editableNotes;
            set
            {
                if (_editableNotes != value)
                {
                    _editableNotes = value;
                    OnPropertyChanged(nameof(EditableNotes));
                }
            }
        }

        public HierarchyItemViewModel(HierarchyNodeDto dto)
        {
            Id = dto.Id;
            ParentNodeId = dto.ParentNodeId;
            NodeType = dto.NodeType;
            Title = dto.Title;
            SortOrder = dto.SortOrder;
            File = dto.File;
            Group = dto.Group;

            _editableTitle = dto.Title;
            _editableDescription = dto.Group?.Description ?? string.Empty;
            _editableNotes = dto.File?.Notes ?? string.Empty;
        }
    }
}