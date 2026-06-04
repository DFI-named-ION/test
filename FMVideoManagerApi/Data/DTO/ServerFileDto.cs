namespace FMVideoManagerApi.Data.DTO
{
    public sealed class ServerFileDto
    {
        public long NodeId { get; set; }

        public long ServerFileItemId { get; set; }

        public long? ParentNodeId { get; set; }

        public string Title { get; set; } = null!;

        public string? OriginalFilename { get; set; }

        public string? Notes { get; set; }

        public string? ContentHash { get; set; }

        public long? SizeBytes { get; set; }

        public string? MimeType { get; set; }

        public long? DurationMs { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}