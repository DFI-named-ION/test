namespace FMVideoManagerApi.Data.DTO.Indexing
{
    public sealed class RegisterLocalFileRequest
    {
        public string ContentHash { get; set; } = null!;

        public string OriginalFilename { get; set; } = null!;

        public string? LocalPath { get; set; }

        public string? LocalDeviceId { get; set; }

        public string? Title { get; set; }

        public long SizeBytes { get; set; }

        public long? DurationMs { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public string? MimeType { get; set; }

        public long? ParentNodeId { get; set; }

        public string? Notes { get; set; }
    }
}