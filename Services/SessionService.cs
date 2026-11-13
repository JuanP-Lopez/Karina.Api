using System.Net.Http.Headers;
using System.Text;
using Karina.Api.Data;
using Karina.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Karina.Api.Services;

public class SessionService : ISessionService
{
    private readonly KarinaContext _context;

    private readonly IConfiguration _configuration;

    private readonly string _agentBaseUrl;

    public SessionService(IConfiguration configuration, KarinaContext context)
    {
        _context = context;
        _configuration = configuration;
        _agentBaseUrl = _configuration["Agent:GeminiUrl"] ?? throw new InvalidOperationException("KarinaAgent:BaseUrl is not configured.");
    }

    public async Task<(bool isSessionCreated, string? session_id)> createSession(string user, Guid userId)
    {

        Console.WriteLine("===Trying To Establish A Session===");

        Guid session = Guid.NewGuid();
        string session_string = session.ToString();

        string url = $"{_agentBaseUrl.TrimEnd('/')}/apps/karina_agent/users/{Uri.EscapeDataString(user)}/sessions/{Uri.EscapeDataString(session_string)}";

        try
        {

            using (HttpClient client = new HttpClient())
            {
                using var content = new StringContent(
                    "{}",
                    Encoding.UTF8,
                    "application/json"
                );
                HttpResponseMessage response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string response_to_json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"===ADK Response: {response_to_json}===");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"===Session Created At: {DateTime.UtcNow:O}===");

                    bool stored = await createSessionInDB(session, userId);

                    if (!stored)
                    {
                        Console.WriteLine("=== Failed To Store Session In Database ===");
                        return (false, null);
                    }

                    bool resultValidation = await validateSession(session_string);

                    if (resultValidation)
                    {
                        Console.WriteLine("=== Session Created And Confirmed ===");
                        return (true, session_string);
                    }

                    Console.WriteLine("=== Session Impossible To Confirm ===");
                    return (false, null);
                }
                else
                {
                    Console.WriteLine($"===Error Creating Session: ({response.StatusCode}) {response_to_json}===");
                    return (false, "invalid");
                }
            }

        }
        catch (Exception sessionError)
        {
            Console.WriteLine($"=== Error Creating Session: {sessionError} ===");
        }

        return (false, session_string);
    }

    public async Task<bool> createSessionInDB(Guid session, Guid userId)
    {
        try
        {
            Console.WriteLine("===Trying to Register Session In Database===");

            Session newSession = new Session
            {
                session_id = session,
                user_id = userId,
                created_at = DateTime.UtcNow,
                ended_at = null,
                last_activity = DateTime.UtcNow
            };

            _context.Sessions.Add(newSession);
            await _context.SaveChangesAsync();

            Console.WriteLine("===Session Registered In Database Succesfully===");
            return true;
        }
        catch (Exception errorSessionBD)
        {
            Console.WriteLine($"===Error At .createSessionInDB: {errorSessionBD}===");
            return false;
        }
    }

    public async Task<bool> validateSession(string session)
    {

        Console.WriteLine($"===Searching Session===");

        bool sessionFounded = await _context.Sessions.AnyAsync(u => u.session_id.ToString() == session);

        Console.WriteLine($"===Session Founded: {sessionFounded}===");

        return sessionFounded ? true : false;
    }

    //TODO: Crear funcion para validar si existe una sesion activa para el usuario
    public async Task<(bool isActive, Guid session)> searchSession(Guid userId)
    {
        Console.WriteLine("===Searching An Active Session===");
        var searchSession = await _context.Sessions.Where(s => s.user_id == userId && s.ended_at == null
        && s.messages.Any(m => m.source == 1))
            .OrderByDescending(s => s.created_at).FirstOrDefaultAsync();

        if (searchSession == null || searchSession.ended_at != null)
        {
            return (false, Guid.NewGuid()); ;
        }

        DateTime now = DateTime.UtcNow;
        TimeSpan maxInactivity = TimeSpan.FromMinutes(60);

        if (now - searchSession.last_activity < maxInactivity)
        {
            Console.WriteLine($"===Active Session Founded: {searchSession.session_id}===");
            return (true, searchSession.session_id);
        }
        else
        {
            await finishSession(searchSession.session_id.ToString());
            return (false, Guid.NewGuid());
        }

    }

    //TODO: Crear funcion para finalizar sesion
    public async Task<bool> finishSession(string session)
    {

        if (!Guid.TryParse(session, out Guid sessionId))
        {
            return false;
        }

        var searchSession = await _context.Sessions.FirstOrDefaultAsync(s => s.session_id == sessionId);

        if (searchSession == null || searchSession.ended_at != null)
        {
            return false;
        }

        DateTime now = DateTime.UtcNow;
        TimeSpan maxInactivity = TimeSpan.FromMinutes(60);

        if (now - searchSession.last_activity < maxInactivity)
        {
            return false;
        }

        searchSession.ended_at = now;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> updateSession(Guid session)
    {
        Console.WriteLine($"===Updating Last Activity For Session: {session.ToString()}===");
        await _context.Sessions.Where(u => u.session_id == session).ExecuteUpdateAsync(p => p.SetProperty(la => la.last_activity, DateTime.UtcNow));
        return true;
    }

    //! Implementacion de la compatibilidad con portal web
    public async Task<(bool isActive, Guid session)> searchWebSession(Guid userId)
    {
        Console.WriteLine("=== Searching An Active Web Session ===");

        var webSession = await _context.Sessions
            .Where(s =>
                s.user_id == userId &&
                s.ended_at == null &&
                s.messages.Any(m => m.source == 2))
            .OrderByDescending(s => s.created_at)
            .FirstOrDefaultAsync();

        if (webSession == null)
        {
            return (false, Guid.NewGuid());
        }

        DateTime now = DateTime.UtcNow;
        TimeSpan maxInactivity = TimeSpan.FromMinutes(60);

        DateTime lastActivity = webSession.last_activity ?? webSession.created_at;

        if (now - lastActivity < maxInactivity)
        {
            Console.WriteLine(
                $"=== Active Web Session Founded: {webSession.session_id} ==="
            );

            return (true, webSession.session_id);
        }

        await finishSession(webSession.session_id.ToString());

        return (false, Guid.NewGuid());
    }
}