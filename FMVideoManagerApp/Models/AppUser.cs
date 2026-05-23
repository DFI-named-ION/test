namespace FMVideoManagerApp.Models
{
    public sealed class AppUser
    {
        public long Id { get; set; }

        public string Login { get; set; } = null!;
        public string? Password { get; set; }
        public string Alias { get; set; } = null!;

        public ICollection<HierarchyNode> HierarchyNodes { get; set; } = new List<HierarchyNode>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<UserPath> UserPaths { get; set; } = new List<UserPath>(); // ?
    }
}