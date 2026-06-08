namespace FMVideoManagerApi.Data.DTO.Hierarchy
{
    public sealed class CopyNodeResponse
    {
        public long NodeId { get; set; }

        public long? ParentNodeId { get; set; }

        public string Title { get; set; } = null!;
    }
}