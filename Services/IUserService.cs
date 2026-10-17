using Karina.Api.Models;

namespace Karina.Api.Services;

public interface IUserService
{
    public Task<bool> handleUserConfirmation();

    public Task<bool> unconfirmedUserRegistration(string name, string wa_id);
}