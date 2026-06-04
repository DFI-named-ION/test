namespace FMVideoManagerApp.ViewModels.Items
{
    public sealed class HierarchyParentOptionViewModel
    {
        public long? NodeId { get; }

        public string Title { get; }

        public string DisplayTitle => NodeId == null ? "Root" : Title;

        public bool IsRoot => NodeId == null;

        public HierarchyParentOptionViewModel(long? nodeId, string title)
        {
            NodeId = nodeId;
            Title = title;
        }
    }
}