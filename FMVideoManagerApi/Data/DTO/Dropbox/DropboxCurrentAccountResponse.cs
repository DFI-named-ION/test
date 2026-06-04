using System.Text.Json.Serialization;

namespace FMVideoManagerApi.Data.DTO.Dropbox
{
    public sealed class DropboxCurrentAccountResponse
    {
        [JsonPropertyName("account_id")]
        public string AccountId { get; set; } = null!;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public DropboxNameInfo? Name { get; set; }
    }
}