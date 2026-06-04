using System.Text.Json.Serialization;

namespace FMVideoManagerApi.Data.DTO.Dropbox
{
    public sealed class DropboxListFolderResponse
    {
        [JsonPropertyName("entries")]
        public List<DropboxFileEntry> Entries { get; set; } = new();

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; } = null!;

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }
}