using System.Text.Json;
using System.Text.Json.Serialization;

namespace Asher.Host.Jsonl
{
    internal sealed class JsonlRequest
    {
        [JsonPropertyName("requestId")]
        public string? RequestId { get; init; }

        [JsonPropertyName("method")]
        public string? Method { get; init; }

        [JsonPropertyName("params")]
        public JsonElement Params { get; init; }
    }
}
