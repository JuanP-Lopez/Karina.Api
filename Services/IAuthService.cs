namespace Karina.Api.Services;

public interface IAuthService
{

    public Task<(bool login, AuthResponse? user)> userLogin(SignIn request);
    public Task<bool> handleUserConfirmation(SignUpRequest request);

    public Task<Guid> unconfirmedUserRegistration(string name, string wa_id);

    public Task<(bool, string)> createConfirmationCode(string wa_id);
}