namespace FMVideoManagerApi.Models
{
    public sealed class CloudProviderAccount
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public CloudProviderType Provider { get; set; }

        public string ProviderAccountId { get; set; } = null!;

        public string? DisplayName { get; set; }
        public string? Email { get; set; }

        public string AccessTokenEncrypted { get; set; } = null!;
        public string? RefreshTokenEncrypted { get; set; }

        public DateTime? TokenExpiresAtUtc { get; set; }

        public string? Scopes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? LastSyncAtUtc { get; set; }

        public AppUser User { get; set; } = null!;

        public ICollection<StorageReference> StorageReferences { get; set; } = new List<StorageReference>();
    }
}