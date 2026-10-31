using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Karina.Api.Data;
using Karina.Api.DTOs.KarinaAgent;
using Karina.Api.Models;
// using Karina.Api.Models.Whatsapp;

namespace Karina.Api.Services;

public class CommunicationService : ICommunicationService
{
    private readonly KarinaContext _context;

    private readonly ISessionService _sessionService;

    public CommunicationService(KarinaContext context, ISessionService sessionService)
    {
        _context = context;
        _sessionService = sessionService;
    }


    //TODO: Agregar soporte para recibir el mensaje real del usuario
    public async Task<bool> sendMessage(string name, string phoneNumberId, string phoneNumber, string sessionId, string wa_id, string userMessage)
    {
        Console.WriteLine("===Trying To Send User's Message to AI===");

        string url = "http://127.0.0.1:8000/run/";

        var request = new KarinaRequest
        {
            app_name = "karina_agent",
            user_id = name,
            session_id = sessionId,
            new_message = new NewMessage
            {
                role = "user",
                parts = [
                    new Parts {
                        text = userMessage
                    }
                ]
            }
        };

        string? message = request?.new_message?.parts?.FirstOrDefault()?.text;
        var resultStoreMessage = await storeMessage(message, sessionId, wa_id);

        Guid message_id = resultStoreMessage.messageId;

        try
        {

            using (HttpClient client = new HttpClient())
            {
                string message_json = JsonSerializer.Serialize(request);
                using var content = new StringContent(
                    message_json,
                    Encoding.UTF8,
                    "application/json"
                );

                HttpResponseMessage response = await client.PostAsync(url, content);
                // response.EnsureSuccessStatusCode();

                string response_to_json = await response.Content.ReadAsStringAsync();

                List<KarinaResponse>? result = JsonSerializer.Deserialize<List<KarinaResponse>>(response_to_json);

                string? responseText = result?.Where(e => e.Content != null)
                    .SelectMany(e => e.Content!.Parts).Select(p => p.Text).LastOrDefault(text => !string.IsNullOrWhiteSpace(text));

                bool resultStoreResponse = await storeResponse(responseText, sessionId, message_id);

                Console.WriteLine($"===ADK Response To Message: {response_to_json}===");

                Console.WriteLine($"===AI Extracted Response: {responseText}===");

                await sendResponse(phoneNumberId, phoneNumber, responseText);
                await _sessionService.updateSession(Guid.Parse(sessionId));
            }

        }
        catch (Exception messageError)
        {
            Console.WriteLine($"===Error Sending Message To AI: {messageError}===");
        }
        return false;
    }

    public async Task<bool> sendResponse(string phoneNumberId, string phoneNumber, string? response)
    {

        Console.WriteLine("===Trying To Send Response===");

        string url = $" https://graph.facebook.com/v25.0/{phoneNumberId}/messages";

        string aiResponse = response ?? "";

        string accessToken = "EAAPttAPoJgcBSztaxC9bjstbSw1S1TcFv7340E1Q1XWTQtzFduoZCtNpriUXHXrvJafzA9Sun0RNPWZB0VVJUdRMjLG27kqMHAh76VBVdwV81lq4j9huDZCqQRwTBZAczRw44922IAqqLLOfUKahq2Tme2NpE949BiNPtUHo6rGG1Wsna8553cqeZAZC8GaEEZARhwup51uOZCm2ZBwiJ3nWzpohvDWpDT9to3gvLCJWFz4riT60ZCarOxwZABs1poLD603ibsFTVoZAZCitUn1Hi6Igx7v0ZD";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phoneNumber,
                type = "text",
                text = new
                {
                    body = aiResponse
                }
            };

            string payload_to_json = JsonSerializer.Serialize(payload);
            var content = new StringContent(payload_to_json, Encoding.UTF8, "application/json");

            HttpResponseMessage result = await client.PostAsync(url, content);
            string result_to_string = await result.Content.ReadAsStringAsync();

            if (result.IsSuccessStatusCode)
            {
                Console.WriteLine($"===Respone Sended at: {DateTime.UtcNow:O}===");
            }
            else
            {
                Console.WriteLine($"===Error Sending Response: ({result.StatusCode}) : {result_to_string} ===");
            }
        }

        return false;
    }

    //TODO: Modificar almacen de mensaje para vincularlo a user_id en tabla Users
    public async Task<(bool stored, Guid messageId)> storeMessage(string? message, string sessionId, string wa_id)
    {

        try
        {
            Console.WriteLine("===Trying To Store Message===");

            var user = _context.Users.FirstOrDefault(u => u.wa_id == wa_id);

            Console.WriteLine($"===User Founded In .storeMessage Function: {user}===");

            if (user != null)
            {
                Guid userId = user.user_id;
                Console.WriteLine($"===user_id: {userId}===");
                Message newMessage = new Message
                {
                    id = Guid.NewGuid(),
                    user_id = userId,
                    session_id = Guid.Parse(sessionId),
                    content = message,
                    source = 1,
                    sent_at = DateTime.UtcNow
                };

                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();
                
                Guid messageId = newMessage.id;
                return (false, messageId);
            }
            else
            {
                return (false, Guid.NewGuid());
            }

        }
        catch (Exception errorStoringMessage)
        {
            Console.WriteLine($"Error Storing Message: {errorStoringMessage}");
            return (false, Guid.NewGuid());
        }

        return (false, Guid.NewGuid());
    }


    //TODO: Agregar soporte para almacenar la respuesta del Agente en la BD
    public async Task<bool> storeResponse(string response, string sessionId, Guid messageId)
    {
        try
        {
            Console.WriteLine("===Trying To Store Response===");

                Response newResponse= new Response
                {
                    id = Guid.NewGuid(),
                    response_to = messageId,
                    session_id = Guid.Parse(sessionId),
                    content = response,
                    source = 1,
                    sent_at = DateTime.UtcNow
                };

                _context.Responses.Add(newResponse);
                await _context.SaveChangesAsync();

        }
        catch (Exception errorStoringResponse)
        {
            Console.WriteLine($"Error Storing Message: {errorStoringResponse}");
            return false;
        }

        return false;
    }
}