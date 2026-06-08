namespace FMVideoManagerApi.Data.DTO.Indexing
{
    public sealed class RegisterLocalFileResponse
    {
        public long ServerFileItemId { get; set; }

        public long NodeId { get; set; }

        public string ContentHash { get; set; } = null!;

        public bool Created { get; set; }

        public string Title { get; set; } = null!;
    }
}