using Karina.Api.Models.Whatsapp;

namespace Karina.Api.Services;

public interface IWhatsAppService
{

    Task<bool> incomingMessage(string payload);
    Task<(bool isUserConfirmedOrNot, string? phoneNumber, string? phoneNumberId, string? wa_id)> isUserUnconfirmed(string payload);
    Task sendRegistrationMessage(string phoneNumber, string phoneNumberId, string name);
    Task<bool> handleConfirmedUser();
    Task sendMessageToAgent(string message);
}