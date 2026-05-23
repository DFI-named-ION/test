namespace FMVideoManagerApi.Models
{
    public sealed class FileItem
    {
        public long NodeId { get; set; }

        public string? ContentHash { get; set; }

        public string? OriginalFilename { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public HierarchyNode Node { get; set; } = null!;

        public FileContent? Content { get; set; }

        public ICollection<StorageReference> StorageReferences { get; set; } = new List<StorageReference>();
    }
}