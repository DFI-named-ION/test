namespace FMVideoManagerApi.Models
{
    public sealed class NodeTag
    {
        public long NodeId { get; set; }
        public long TagId { get; set; }
        public long UserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public HierarchyNode Node { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}