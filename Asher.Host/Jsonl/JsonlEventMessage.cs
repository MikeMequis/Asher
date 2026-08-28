using System.Text.Json.Serialization;

namespace Asher.Host.Jsonl
{
    internal sealed class JsonlEventMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "event";

        [JsonPropertyName("event")]
        public string Event { get; init; } = string.Empty;

        [JsonPropertyName("protocolVersion")]
        public int ProtocolVersion { get; init; } = 1;
    }
}
