namespace FMVideoManagerApp.Data.DTO
{
    public sealed class CloudProviderAccountDto
    {
        public long Id { get; set; }

        public CloudProviderType Provider { get; set; }

        public string ProviderAccountId { get; set; } = null!;

        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public string? Scopes { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? LastSyncAtUtc { get; set; }
    }

    public enum CloudProviderType // rename to StorageProviderType
    {
        Local = 0,
        Dropbox = 1,
        GoogleDrive = 2
    }
}