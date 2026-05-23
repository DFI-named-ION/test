namespace FMVideoManagerApi.Data.DTO
{
    public sealed class RegisterRequest
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Alias { get; set; } = null!;
    }
}