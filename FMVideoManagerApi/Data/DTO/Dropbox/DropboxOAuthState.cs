namespace FMVideoManagerApi.Data.DTO.Dropbox
{
    public sealed class DropboxOAuthState
    {
        public long UserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Nonce { get; set; } = null!;
    }
}