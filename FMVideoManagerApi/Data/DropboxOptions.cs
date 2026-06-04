namespace FMVideoManagerApi.Data
{
    public sealed class DropboxOptions
    {
        public string AppKey { get; set; } = null!;
        public string AppSecret { get; set; } = null!;
        public string RedirectUri { get; set; } = null!;
    }
}