using Karina.Api.DTOs.Webchat;

namespace Karina.Api.Services;

public interface IWebChatService
{
    Task<ChatMessageResponse> sendMessage(Guid userId, string message);
}