using Karina.Api.Models;

namespace Karina.Api.Services;

public interface ISessionService
{
    public Task<(bool isSessionCreated, string? session_id)> createSession(string user);

    public Task<bool> createSessionInBd();

    public bool checkSession(string session);

}