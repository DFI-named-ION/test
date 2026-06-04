using FMVideoManagerApp.Core;
using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.ViewModels.Items
{
    public sealed class FileLibraryItemViewModel : ObservableObject
    {
        public long? LocalFileLocationId { get; init; }

        public long? ServerFileItemId { get; init; }

        public long? ServerNodeId { get; init; }

        public long? ParentNodeId { get; init; }

        public string? ContentHash { get; init; }

        public string Title { get; init; } = null!;

        public string? LocalPath { get; init; }

        public string? OriginalFilename { get; init; }

        public long? SizeBytes { get; init; }

        public long? DurationMs { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public string? Notes { get; init; }

        public string? PreviewPath { get; init; }

        public bool IsIndexingFailed { get; init; }

        public string? IndexingError { get; init; }

        public LocalFileSyncState? LocalSyncState { get; init; }

        public string SizeText =>
            SizeBytes == null
                ? "Unknown size"
                : SizeConverter.ToHumanReadable(SizeBytes.Value);

        public string Resolution =>
            Width == null || Height == null
                ? "Unknown resolution"
                : $"{Width}x{Height}";

        public string Duration
        {
            get
            {
                if (DurationMs == null)
                    return "Unknown Duration";

                TimeSpan t = TimeSpan.FromMilliseconds(DurationMs.Value);

                return t.ToString(@"hh\:mm\:ss\.fff");
            }
        }

        public string DisplayPreviewPath =>
            string.IsNullOrWhiteSpace(PreviewPath)
                ? "/Resources/Images/image_placeholder.png"
                : PreviewPath;

        public bool IsAvailableLocally => !string.IsNullOrWhiteSpace(LocalPath);

        public bool IsKnownByServer => ServerFileItemId != null;

        public string AvailabilityText
        {
            get
            {
                if (IsIndexingFailed)
                    return "Indexing failed";

                if (IsAvailableLocally && IsKnownByServer)
                    return "Available locally";

                if (IsAvailableLocally && !IsKnownByServer)
                    return "Local only";

                if (!IsAvailableLocally && IsKnownByServer)
                    return "Remote only";

                return "Unknown";
            }
        }

        public string SyncStatusText
        {
            get
            {
                if (IsIndexingFailed)
                    return "Indexing failed";

                return LocalSyncState switch
                {
                    LocalFileSyncState.LocalOnly => "Local only",
                    LocalFileSyncState.PendingSync => "Pending sync",
                    LocalFileSyncState.Synced => "Synced",
                    LocalFileSyncState.SyncFailed => "Sync failed",
                    null when IsKnownByServer => "Server file",
                    _ => "Unknown"
                };
            }
        }

        public string Path => LocalPath ?? "Cloud owned"; // ????

        public string Data => $"" +
            $"LocalFileLocationId: {LocalFileLocationId}\n" +
            $"ServerFileItemId: {ServerFileItemId}\n" +
            $"ServerNodeId: {ServerNodeId}\n" +
            $"ParentNodeId: {ParentNodeId}\n" +
            $"ContentHash: {ContentHash}\n" +
            $"Title: {Title}\n" +
            $"LocalPath: {LocalPath}\n" +
            $"OriginalFilename: {OriginalFilename}\n" +
            $"SizeBytes: {SizeBytes}\n" +
            $"DurationMs: {DurationMs}\n" +
            $"Width: {Width}\n" +
            $"Height: {Height}\n" +
            $"PreviewPath: {PreviewPath}\n" +
            $"IsIndexingFailed: {IsIndexingFailed}\n" +
            $"IndexingError: {IndexingError}\n" +
            $"LocalSyncState: {LocalSyncState}\n" +
            $"SizeText: {SizeText}\n" +
            $"Resolution: {Resolution}\n" +
            $"Duration: {Duration}\n" +
            $"DisplayPreviewPath: {DisplayPreviewPath}\n" +
            $"IsAvailableLocally: {IsAvailableLocally}\n" +
            $"IsKnownByServer: {IsKnownByServer}\n" +
            $"AvailabilityText: {AvailabilityText}\n" +
            $"SyncStatusText: {SyncStatusText}\n" +
            $"Path: {Path}\n";
    }
}