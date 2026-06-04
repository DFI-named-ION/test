namespace FMVideoManagerApi.Data.DTO.Hierarchy
{
    public sealed class CreateGroupRequest
    {
        public long? ParentNodeId { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }
    }
}