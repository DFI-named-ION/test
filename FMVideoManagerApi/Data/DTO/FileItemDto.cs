namespace FMVideoManagerApi.Data.DTO
{
    public sealed class FileItemDto
    {
        public long NodeId { get; set; }
        public string Title { get; set; } = null!;
        public string? ContentHash { get; set; }
        public string? OriginalFilename { get; set; }
        public long? SizeBytes { get; set; }
        public long? DurationMs { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}