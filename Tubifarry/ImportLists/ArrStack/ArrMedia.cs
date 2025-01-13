using System.Text.Json.Serialization;

namespace Tubifarry.ImportLists.ArrStack
{
    record class ArrMedia
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }
}
