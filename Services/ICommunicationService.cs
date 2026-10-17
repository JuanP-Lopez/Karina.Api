namespace Karina.Api.Services;

public interface ICommunicationService
{
    public Task<bool> sendMessage(string phoneNumberId, string phoneNumber, string sessionId);

    public Task<bool> sendResponse(string phoneNumberId, string phoneNumber, string response);

}