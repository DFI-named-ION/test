namespace FMVideoManagerApp.Models.Local
{
    public sealed class LocalIndexedPath
    {
        public long Id { get; set; }

        public long ServerUserId { get; set; }

        public string Path { get; set; } = null!;

        public bool IsEnabled { get; set; } = true;

        public bool IncludeSubdirectories { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? LastScannedAtUtc { get; set; }
    }
}