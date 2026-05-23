namespace FMVideoManagerApp.Models
{
    public sealed class GroupItem
    {
        public long NodeId { get; set; }

        public string NodeType { get; set; } = NodeTypes.Group;

        public string? Description { get; set; }
        public string? BackgroundColorHex { get; set; }
        public string? ForegroundColorHex { get; set; }

        public HierarchyNode Node { get; set; } = null!;
    }
}