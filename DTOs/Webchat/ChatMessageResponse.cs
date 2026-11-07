namespace Karina.Api.DTOs.Webchat;

public class ChatMessageResponse
{
    public Guid messageId { get; set; }
    public Guid responseId { get; set; }
    public Guid sessionId { get; set; }
    public string content { get; set; } = "";
    public DateTime sentAt { get; set; }
}