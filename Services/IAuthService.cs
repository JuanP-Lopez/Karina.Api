namespace Karina.Api.Services;

public interface IAuthService
{
    public Task<bool> handleUserConfirmation();

    public Task<bool> unconfirmedUserRegistration(string name, string wa_id);

    public Task<bool> createConfirmationCode(Guid user_id, string wa_id);
}