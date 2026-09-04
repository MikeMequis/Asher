using System.Text.Json.Serialization;

namespace Asher.Host.Jsonl
{
    internal sealed class JsonlResponse
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "response";

        [JsonPropertyName("requestId")]
        public string? RequestId { get; init; }

        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Result { get; init; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonlError? Error { get; init; }
    }
}
