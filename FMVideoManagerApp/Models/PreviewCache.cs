namespace FMVideoManagerApp.Models
{
    public sealed class PreviewCache
    {
        public long Id { get; set; }

        public long ServerUserId { get; set; }

        public long? ServerFileItemId { get; set; }

        public string? ContentHash { get; set; }

        public string PreviewPath { get; set; } = null!;

        public int? Width { get; set; }

        public int? Height { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime LastAccessedAtUtc { get; set; }
    }
}