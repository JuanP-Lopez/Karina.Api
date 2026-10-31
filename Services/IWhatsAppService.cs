using Karina.Api.Models.Whatsapp;

namespace Karina.Api.Services;

public interface IWhatsAppService
{

    Task<bool> incomingMessage(string payload);

    Task<(bool userFoundedOrNot, int role, Guid user_id)> isUserRegistered(string payload);

    Task sendRegistrationMessage(string phoneNumber, string phoneNumberId, string name, string code);

}