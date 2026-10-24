namespace Karina.Api.Services;

public interface ICommunicationService
{
    public Task<bool> sendMessage(string name, string phoneNumberId, string phoneNumber, string sessionId, string wa_id, string userMessage);

    public Task<bool> sendResponse(string phoneNumberId, string phoneNumber, string response);

    public Task<(bool stored, Guid messageId)> storeMessage(string message, string sessionId, string wa_id);

    public Task<bool> storeResponse(string response, string sessionId, Guid messageId);

}