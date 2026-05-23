namespace FMVideoManagerApp.Models.Local
{
    public sealed class LocalFileLocation
    {
        public long Id { get; set; }

        public long ServerUserId { get; set; }

        public long? ServerFileItemId { get; set; }

        public string? ContentHash { get; set; }

        public long? LocalIndexedPathId { get; set; }

        public string Path { get; set; } = null!;

        public string Filename { get; set; } = null!;

        public long SizeBytes { get; set; }

        public DateTime LastModifiedUtc { get; set; }

        public DateTime LastSeenUtc { get; set; }

        public bool ExistsOnDisk { get; set; } = true;

        public LocalIndexedPath? LocalIndexedPath { get; set; }
    }
}