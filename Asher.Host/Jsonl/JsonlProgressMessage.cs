using System.Text.Json.Serialization;

namespace Asher.Host.Jsonl
{
    internal sealed class JsonlProgressMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "progress";

        [JsonPropertyName("requestId")]
        public string RequestId { get; init; } = string.Empty;

        [JsonPropertyName("progress")]
        public object Progress { get; init; } = new();
    }
}
