namespace FMVideoManagerApp.Data.DTO
{
    public sealed class DropboxIndexResult
    {
        public long? CloudProviderAccountId { get; set; }

        public int FoundMp4Files { get; set; }

        public int DownloadedFiles { get; set; }

        public int IndexedFiles { get; set; }

        public int FailedFiles { get; set; }

        public int MarkedMissing { get; set; }
    }
}