namespace FMVideoManagerApp.Data.DTO
{
    public sealed class AuthResponse
    {
        public long UserId { get; set; }
        public string Login { get; set; } = null!;
        public string Alias { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
    }
}