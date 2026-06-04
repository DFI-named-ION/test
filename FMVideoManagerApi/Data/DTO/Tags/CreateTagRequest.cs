namespace FMVideoManagerApi.Data.DTO.Tags
{
    public sealed class CreateTagRequest
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? BackgroundColorHex { get; set; }

        public string? ForegroundColorHex { get; set; }
    }
}