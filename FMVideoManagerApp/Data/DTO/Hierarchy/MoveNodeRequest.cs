namespace FMVideoManagerApp.Data.DTO.Hierarchy
{
    public sealed class MoveNodeRequest
    {
        public long? NewParentNodeId { get; set; }

        public int? SortOrder { get; set; }
    }
}