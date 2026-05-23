namespace FMVideoManagerApi.Models
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string Secret { get; set; } = null!;
        public int AccessTokenMinutes { get; set; } = 60;
    }
}