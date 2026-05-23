namespace FMVideoManagerApi.Models
{
    public sealed class AppUser
    {
        public long Id { get; set; }

        public string Login { get; set; } = null!;
        public string Alias { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public ICollection<HierarchyNode> HierarchyNodes { get; set; } = new List<HierarchyNode>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<CloudProviderAccount> CloudProviderAccounts { get; set; } = new List<CloudProviderAccount>();
    }
}