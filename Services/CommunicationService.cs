using System.Net.Http.Headers;
using Karina.Api.Services;
using System.Text;
using System.Text.Json;
using Karina.Api.Data;
using Karina.Api.DTOs.KarinaAgent;
using Karina.Api.Models;

namespace Karina.Api.Services;

public class CommunicationService : ICommunicationService
{
    private readonly KarinaContext _context;

    private readonly ISessionService _sessionService;

    private readonly IUserStatisticsService _userStatisticsService;

    private readonly IConfiguration _configuration;

    private readonly string _accessToken;

    private readonly string _agentBaseUrl;

    public CommunicationService(IConfiguration configuration, KarinaContext context, ISessionService sessionService, IUserStatisticsService userStatisticsService)
    {
        _context = context;
        _sessionService = sessionService;
        _userStatisticsService = userStatisticsService;
        _configuration = configuration;
        _accessToken = _configuration["Meta:WhatsappToken"];
        _agentBaseUrl = _configuration["Agent:GeminiUrl"] ?? throw new InvalidOperationException("KarinaAgent:BaseUrl is not configured.");
    }


    //TODO: Agregar soporte para recibir el mensaje real del usuario
    public async Task<bool> sendMessage(string name, string phoneNumberId, string phoneNumber, string sessionId, string wa_id, string userMessage)
    {
        Console.WriteLine("===Trying To Send User's Message to AI===");

        Console.WriteLine("===Recovering User Preferences===");
        var user = _context.Users.FirstOrDefault(u => u.wa_id == wa_id);
        var preferences = await _userStatisticsService.getUserPreferences(user.user_id);

        string url = $"{_agentBaseUrl.TrimEnd('/')}/run/";

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
                        text = $"[Preferencias] Apodo: {preferences.nickname} Tono: {preferences.tone} Personalidad: {preferences.language} [Mensaje] {userMessage}"
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

        // string accessToken = _configuration["Meta:WhatsappToken"];

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

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

            Response newResponse = new Response
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

    //! Implementacion de compatibilidad con portal web
    // Genera una respuesta del agente para el portal web
    public async Task<string?> generateResponse(
        Guid userId,
        string agentUserId,
        string sessionId,
        string userMessage)
    {
        Console.WriteLine("=== Generating Web Chat Response ===");

        try
        {
            Console.WriteLine("=== Recovering User Preferences ===");

            var preferences = await _userStatisticsService
                .getUserPreferences(userId);

            string url = $"{_agentBaseUrl.TrimEnd('/')}/run/";

            var request = new KarinaRequest
            {
                app_name = "karina_agent",
                user_id = agentUserId,
                session_id = sessionId,
                new_message = new NewMessage
                {
                    role = "user",
                    parts =
                    [
                        new Parts
                    {
                        text = $"[Preferencias] Apodo: {preferences?.nickname} Tono: {preferences?.tone} Personalidad: {preferences?.language} [Mensaje] {userMessage}"
                    }
                    ]
                }
            };

            using HttpClient client = new HttpClient();

            string messageJson = JsonSerializer.Serialize(request);

            using var content = new StringContent(
                messageJson,
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await client.PostAsync(url, content);

            string responseJson = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"=== ADK Status: {response.StatusCode} ===");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"=== ADK Error: {responseJson} ===");
                return null;
            }

            List<KarinaResponse>? result =
                JsonSerializer.Deserialize<List<KarinaResponse>>(responseJson);

            string? responseText = result?
                .Where(e => e.Content != null)
                .SelectMany(e => e.Content!.Parts)
                .Select(p => p.Text)
                .LastOrDefault(text => !string.IsNullOrWhiteSpace(text));

            if (string.IsNullOrWhiteSpace(responseText))
            {
                Console.WriteLine("=== ADK Returned An Empty Response ===");
                return null;
            }

            Console.WriteLine($"=== AI Response: {responseText} ===");

            return responseText;
        }
        catch (Exception error)
        {
            Console.WriteLine($"=== Error Generating AI Response: {error} ===");
            return null;
        }
    }

}