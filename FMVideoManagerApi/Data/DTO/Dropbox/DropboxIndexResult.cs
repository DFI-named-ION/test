namespace FMVideoManagerApi.Data.DTO.Dropbox
{
    public sealed class DropboxIndexResult
    {
        public long? CloudProviderAccountId { get; set; }

        public int FoundFiles { get; set; }

        public int DownloadedFiles { get; set; }

        public int IndexedFiles { get; set; }

        public int FailedFiles { get; set; }

        public int MarkedMissing { get; set; }
    }
}