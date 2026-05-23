namespace FMVideoManagerApp.Models
{
    public sealed class UserPath
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public string Path { get; set; }

        public AppUser User { get; set; } = null!;
    }
}