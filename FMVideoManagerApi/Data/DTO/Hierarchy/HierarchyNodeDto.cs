using FMVideoManagerApi.Models;

namespace FMVideoManagerApi.Data.DTO.Hierarchy
{
    public sealed class HierarchyNodeDto
    {
        public long Id { get; set; }

        public long? ParentNodeId { get; set; }

        public HierarchyNodeType NodeType { get; set; }

        public string Title { get; set; } = null!;

        public int SortOrder { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public FileNodeDto? File { get; set; }

        public GroupNodeDto? Group { get; set; }
    }
}