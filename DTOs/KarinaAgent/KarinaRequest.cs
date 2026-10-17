using System.Text.Json.Serialization;

namespace Karina.Api.DTOs.KarinaAgent;

public class KarinaRequest
{
    [JsonPropertyName("app_name")]
    public string app_name { get; set; } = "";

    [JsonPropertyName("user_id")]
    public string user_id { get; set; } = "";

    [JsonPropertyName("session_id")]
    public string session_id { get; set; } = "";

    [JsonPropertyName("new_message")]
    public NewMessage? new_message { get; set; }
}

public class NewMessage
{
    [JsonPropertyName("role")]
    public string role { get; set; } = "";

    [JsonPropertyName("parts")]
    public List<Parts>? parts { get; set; }
}

public class Parts
{
    [JsonPropertyName("text")]
    public string text { get; set; } = "";
}