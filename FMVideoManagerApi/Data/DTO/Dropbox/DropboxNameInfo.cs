using System.Text.Json.Serialization;

namespace FMVideoManagerApi.Data.DTO.Dropbox
{
    public sealed class DropboxNameInfo
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}