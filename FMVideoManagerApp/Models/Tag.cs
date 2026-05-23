namespace FMVideoManagerApp.Models
{
    public sealed class Tag
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public string? BackgroundColorHex { get; set; }
        public string? ForegroundColorHex { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public AppUser User { get; set; } = null!;

        public ICollection<TagAlias> Aliases { get; set; } = new List<TagAlias>();
        public ICollection<NodeTag> NodeTags { get; set; } = new List<NodeTag>();
    }
}