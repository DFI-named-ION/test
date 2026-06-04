namespace FMVideoManagerApi.Data.DTO.Hierarchy
{
    public sealed class FileNodeDto
    {
        public long NodeId { get; set; }

        public string? ContentHash { get; set; }

        public string? OriginalFilename { get; set; }

        public string? Notes { get; set; }

        public long? SizeBytes { get; set; }

        public string? MimeType { get; set; }

        public long? DurationMs { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }
    }
}