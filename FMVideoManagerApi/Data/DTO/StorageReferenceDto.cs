using FMVideoManagerApi.Models;

namespace FMVideoManagerApi.Data.DTO
{
    public sealed class StorageReferenceDto
    {
        public long Id { get; set; }

        public long? FileNodeId { get; set; }

        public string? ContentHash { get; set; }

        public long? CloudProviderAccountId { get; set; }

        public CloudProviderType Provider { get; set; }

        public string ProviderItemId { get; set; } = null!;

        public string? ProviderPath { get; set; }

        public string Name { get; set; } = null!;

        public string? ProviderRevision { get; set; }

        public string? MimeType { get; set; }

        public long? SizeBytes { get; set; }

        public DateTime? ProviderModifiedAtUtc { get; set; }

        public DateTime LastSeenAtUtc { get; set; }

        public StorageReferenceState State { get; set; }

        public string? AccountDisplayName { get; set; }

        public string? AccountEmail { get; set; }
    }
}
