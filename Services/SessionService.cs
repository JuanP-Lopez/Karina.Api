using System.Net.Http.Headers;
using System.Text;
using Karina.Api.Data;
using Karina.Api.Models;

namespace Karina.Api.Services;

public class SessionService : ISessionService
{
    private readonly KarinaContext _context;

    public SessionService(KarinaContext context)
    {
        _context = context;
    }

    public async Task<(bool isSessionCreated, string? session_id)> createSession(string user)
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

                    Session newSession = new Session
                    {
                        session_id = session,
                        user_id = Guid.Parse("dfc7f9b2-6464-4f74-a5cb-7649c636ee01"),
                        created_at = DateTime.UtcNow
                    };

                    _context.Sessions.Add(newSession);
                    await _context.SaveChangesAsync();

                    // await _context.Sessions.AddAsync(session);

                    // bool sessionConfirmed = checkSession(session);

                    // if (sessionConfirmed == true)
                    // {   
                    //     Console.WriteLine($"===Session Created And Confirmed===");
                    //     return (true, session);
                    // } else
                    // {
                    //     Console.WriteLine($"===Session Impossible To Confirm===");
                    //     return (false, session);
                    // }
                    return (true, session_string);
                } else
                {
                    Console.WriteLine($"===Error Creating Session: ({response.StatusCode}) {response_to_json}===");
                    return (false, session_string);
                }
            }

        } catch (Exception sessionError)
        {
            Console.WriteLine($"=== Error Creating Session: {sessionError} ===");
        }

        return (false, session_string);
    }

    public async Task<bool> createSessionInBd()
    {
        return true;
    }

    public bool checkSession(string session)
    {

        Console.WriteLine($"===Searching Session===");

        bool sessionFounded = _context.Sessions.Any(u => u.session_id.ToString() == session);

        Console.WriteLine($"===Session Founded: {sessionFounded}===");

        return sessionFounded ? true : false;
    }

}