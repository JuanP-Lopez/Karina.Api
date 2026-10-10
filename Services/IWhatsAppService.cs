using Karina.Api.Models.Whatsapp;

namespace Karina.Api.Services;

public interface IWhatsAppService
{

    Task incomingMessage(string payload);

    Task<bool> isUserRegistered(string payload);
}