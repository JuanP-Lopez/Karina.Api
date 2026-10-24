using System.Net.Http.Headers;
using System.Text;
using Karina.Api.Data;
using Karina.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Karina.Api.Services;

public class SessionService : ISessionService
{
    private readonly KarinaContext _context;

    public SessionService(KarinaContext context)
    {
        _context = context;
    }

    public async Task<(bool isSessionCreated, string? session_id)> createSession(string user, Guid userId)
    {

        Console.WriteLine("===Trying To Establish A Session===");

        Guid session = Guid.NewGuid();
        string session_string = session.ToString();

        string url = $"http://127.0.0.1:8000/apps/karina_agent/users/{Uri.EscapeDataString(user)}/sessions/{Uri.EscapeDataString(session_string)}";

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

                    var result = await createSessionInDB(session, userId);

                    bool resultValidation = await validateSession(session_string);

                    if (resultValidation == true)
                    {
                        Console.WriteLine($"===Session Created And Confirmed===");
                        return (true, session_string);
                    }
                    else
                    {
                        Console.WriteLine($"===Session Impossible To Confirm===");
                        return (false, "invalid");
                    }
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

        bool sessionFounded = _context.Sessions.Any(u => u.session_id.ToString() == session);

        Console.WriteLine($"===Session Founded: {sessionFounded}===");

        return sessionFounded ? true : false;
    }

    //TODO: Crear funcion para validar si existe una sesion activa para el usuario
    public async Task<(bool isActive, Guid session)> searchSession(Guid userId)
    {   
        Console.WriteLine("===Searching An Active Session===");
        var searchSession = await _context.Sessions.Where(s => s.user_id == userId && s.ended_at == null)
            .OrderByDescending(s => s.created_at).FirstOrDefaultAsync();
        
        if (searchSession == null || searchSession.ended_at != null)
        {
            return (false, Guid.NewGuid());;
        }

        DateTime now = DateTime.UtcNow;
        TimeSpan maxInactivity = TimeSpan.FromMinutes(60);

        if (now - searchSession.last_activity < maxInactivity)
        {
            Console.WriteLine($"===Active Session Founded: {searchSession.session_id}===");
            return (true, searchSession.session_id);
        } else
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
        _context.Sessions.Where(u => u.session_id == session).ExecuteUpdate(p => p.SetProperty(la => la.last_activity, DateTime.UtcNow));
        return true;
    }
}