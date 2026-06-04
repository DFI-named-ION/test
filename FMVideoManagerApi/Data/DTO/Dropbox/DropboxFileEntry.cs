using System.Text.Json.Serialization;

namespace FMVideoManagerApi.Data.DTO.Dropbox
{
    public sealed class DropboxFileEntry
    {
        [JsonPropertyName(".tag")]
        public string Tag { get; set; } = null!;

        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("path_lower")]
        public string? PathLower { get; set; }

        [JsonPropertyName("path_display")]
        public string? PathDisplay { get; set; }

        [JsonPropertyName("rev")]
        public string? Rev { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("client_modified")]
        public DateTime? ClientModified { get; set; }

        [JsonPropertyName("server_modified")]
        public DateTime? ServerModified { get; set; }

        [JsonPropertyName("content_hash")]
        public string? ContentHash { get; set; }
    }
}