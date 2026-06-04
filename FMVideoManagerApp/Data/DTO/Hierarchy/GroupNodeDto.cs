namespace FMVideoManagerApp.Data.DTO.Hierarchy
{
    public sealed class GroupNodeDto
    {
        public long NodeId { get; set; }

        public string? Description { get; set; }

        public string? BackgroundColorHex { get; set; }

        public string? ForegroundColorHex { get; set; }
    }
}