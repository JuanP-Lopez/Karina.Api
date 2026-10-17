using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Karina.Api.Data;
using Karina.Api.DTOs.KarinaAgent;
using Karina.Api.Models;

namespace Karina.Api.Services;

public class CommunicationService : ICommunicationService
{
    private readonly KarinaContext _context;

    public CommunicationService(KarinaContext context)
    {
        _context = context;
    }

    public async Task<bool> sendMessage(string phoneNumberId, string phoneNumber, string sessionId)
    {
        Console.WriteLine("===Trying To Send User's Message to AI===");

        string url = "http://127.0.0.1:8000/run/";

        var request = new KarinaRequest
        {
            app_name = "karina_agent",
            user_id = "prueba",
            session_id = sessionId,
            new_message = new NewMessage
            {
                role = "user",
                parts = [
                    new Parts {
                        text = "Hola"
                    }
                ]
            }
        };

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

                Console.WriteLine($"===ADK Response To Message: {response_to_json}===");

                Console.WriteLine($"===AI Extracted Response: {responseText}===");

                await sendResponse(phoneNumberId, phoneNumber, responseText);
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

        string accessToken = "EAAPttAPoJgcBSwXUv20oyexe5ZBCVdXZAGVIcyjpZCmNZAjIZCiCqMn8NnsAqM5MRKMzzQzGuBpFwM1qn6nHzXApE4YqEFQ4tfZBw8cfXmcnCZANJrkPeEYyuL6Cy4fOQhx2rk9PkRVIoLuQPllzQIOWKGstZBzNrE7CID4LVfetW3tJXhv0yQZCdVkhWAoz7tQ6dRQrUNstG79rlgolrvFnIr1A4k0fFOznJUo1BHjsbAV0xgeDZCaeWgZBsTc6PIKftiW5B2JGVOhEJfmfgnpbkmEKFUZD";

        //! A futuro, podria ser util que un AI Agent se encarge de redactar el mensaje segun el idioma en que llega el mensaje del usuario
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
            } else
            {
                Console.WriteLine($"===Error Sending Response: ({result.StatusCode}) : {result_to_string} ===");
            }
        }

        return false;
    }
}