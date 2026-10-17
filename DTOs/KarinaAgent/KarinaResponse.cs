
using System.Text.Json.Serialization;

namespace Karina.Api.DTOs.KarinaAgent;

public class KarinaResponse
{
    [JsonPropertyName("content")]
    public ContentResponse? Content { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("modelVersion")]
    public string? ModelVersion { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("invocationId")]
    public string? InvocationId { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("timestamp")]
    public double? Timestamp { get; set; }
}

public class ContentResponse
{
    [JsonPropertyName("parts")]
    public List<PartsResponse> Parts { get; set; } = [];

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class PartsResponse
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("thoughtSignature")]
    public string? ThoughtSignature { get; set; }
}
