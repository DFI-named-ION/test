namespace FMVideoManagerApi.Models
{
    public sealed class TagAlias
    {
        public long Id { get; set; }

        public long TagId { get; set; }
        public long UserId { get; set; }

        public string Alias { get; set; } = null!;

        public Tag Tag { get; set; } = null!;
    }
}