using FMVideoManagerApp.Core;

namespace FMVideoManagerApp.Models
{
    public sealed class FileItem
    {
        public long NodeId { get; set; }

        public string NodeType { get; set; } = NodeTypes.File;

        public string Hash { get; set; } = null!;
        public DateTime UploadDate { get; set; }

        public long SizeBytes { get; set; }
        public string Path { get; set; } = null!;
        public string? OriginalFilename { get; set; }

        public long? DurationMs { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        public string? Notes { get; set; }

        public HierarchyNode Node { get; set; } = null!;

        public string Resolution => $"{Width}x{Height}";
        public string Size => SizeConverter.ToHumanReadable(SizeBytes);
        public string Lenght
        {
            get
            {
                var t = TimeSpan.FromMilliseconds(DurationMs.GetValueOrDefault());
                return string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                        t.Hours,
                        t.Minutes,
                        t.Seconds,
                        t.Milliseconds);
            }
        }
    }
}