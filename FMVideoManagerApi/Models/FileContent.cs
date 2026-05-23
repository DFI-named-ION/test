namespace FMVideoManagerApi.Models
{
    public sealed class FileContent
    {
        public string Hash { get; set; } = null!;

        // public string HashAlgorithm { get; set; } = "SHA256";

        public long SizeBytes { get; set; }

        public string? MimeType { get; set; }

        public long? DurationMs { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public ICollection<FileItem> FileItems { get; set; } = new List<FileItem>();

        public ICollection<StorageReference> StorageReferences { get; set; } = new List<StorageReference>();
    }
}