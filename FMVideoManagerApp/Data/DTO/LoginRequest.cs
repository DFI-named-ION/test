namespace FMVideoManagerApp.Data.DTO
{
    public sealed class LoginRequest
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}