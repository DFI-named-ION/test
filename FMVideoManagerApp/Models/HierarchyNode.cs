namespace FMVideoManagerApp.Models
{
    public sealed class HierarchyNode
    {
        public long Id { get; set; }

        public long UserId { get; set; }
        public long? ParentNodeId { get; set; }

        public string NodeType { get; set; } = null!;
        public string Title { get; set; } = null!;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public AppUser User { get; set; } = null!;

        public HierarchyNode? ParentNode { get; set; }
        public ICollection<HierarchyNode> ChildNodes { get; set; } = new List<HierarchyNode>();

        public GroupItem? GroupItem { get; set; }
        public FileItem? FileItem { get; set; }

        public ICollection<NodeAlias> Aliases { get; set; } = new List<NodeAlias>();
        public ICollection<NodeTag> NodeTags { get; set; } = new List<NodeTag>();
    }
}