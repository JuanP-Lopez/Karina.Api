using Karina.Api.Models;

namespace Karina.Api.Services;

public interface ISessionService
{
    public Task<(bool isSessionCreated, string? session_id)> createSession(string user, Guid userId);

    public Task<bool> createSessionInDB(Guid session, Guid userId);

    public Task<bool> validateSession(string session);

    public Task<(bool isActive, Guid session)> searchSession(Guid session);

    public Task<bool> finishSession(string session);

    public Task<bool> updateSession(Guid session);

}