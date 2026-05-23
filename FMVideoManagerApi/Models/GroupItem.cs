namespace FMVideoManagerApi.Models
{
    public sealed class GroupItem
    {
        public long NodeId { get; set; }

        public string? Description { get; set; }

        public string? BackgroundColorHex { get; set; }
        public string? ForegroundColorHex { get; set; }

        public HierarchyNode Node { get; set; } = null!;
    }
}