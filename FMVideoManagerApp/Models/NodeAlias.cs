namespace FMVideoManagerApp.Models
{
    public sealed class NodeAlias
    {
        public long Id { get; set; }

        public long NodeId { get; set; }

        public string Alias { get; set; } = null!;

        public HierarchyNode Node { get; set; } = null!;
    }
}