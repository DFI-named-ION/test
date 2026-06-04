namespace FMVideoManagerApi.Data.DTO.Tags
{
    public sealed class TagDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? BackgroundColorHex { get; set; }

        public string? ForegroundColorHex { get; set; }
    }
}