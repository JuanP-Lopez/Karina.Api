using Karina.Api.Data;
using Karina.Api.DTOs.Webchat;
using Karina.Api.Models;

namespace Karina.Api.Services;

public class WebChatService : IWebChatService
{
    private readonly KarinaContext _context;
    private readonly ISessionService _sessionService;
    private readonly ICommunicationService _communicationService;

    public WebChatService(
        KarinaContext context,
        ISessionService sessionService,
        ICommunicationService communicationService)
    {
        _context = context;
        _sessionService = sessionService;
        _communicationService = communicationService;
    }

    public async Task<ChatMessageResponse> sendMessage(
        Guid userId,
        string message)
    {
        Console.WriteLine("=== Processing Web Chat Message ===");

        // 1. Validate the incoming message
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.");

        if (message.Length > 4000)
            throw new ArgumentException("Message exceeds the maximum length.");

        // 2. Establish a stable identity for the web agent
        string agentUserId = $"web_{userId:N}";

        // 3. Search for an active web session
        Console.WriteLine("=== Searching Web Session ===");

        var (isActive, sessionId) =
            await _sessionService.searchWebSession(userId);

        // 4. Create a session if none exists
        if (!isActive)
        {
            Console.WriteLine("=== Creating New Web Session ===");

            var (created, newSessionId) =
                await _sessionService.createSession(agentUserId, userId);

            if (!created || !Guid.TryParse(newSessionId, out sessionId))
                throw new InvalidOperationException(
                    "Could not create a web chat session.");
        }

        // 5. Store the user's message
        Console.WriteLine("=== Storing Web Message ===");

        Message newMessage = new Message
        {
            id = Guid.NewGuid(),
            user_id = userId,
            session_id = sessionId,
            content = message,
            source = 2,
            sent_at = DateTime.UtcNow
        };

        _context.Messages.Add(newMessage);

        await _context.SaveChangesAsync();

        // 6. Request a response from Gemini ADK
        Console.WriteLine("=== Requesting AI Response ===");

        string? responseText =
            await _communicationService.generateResponse(
                userId,
                agentUserId,
                sessionId.ToString(),
                message
            );

        if (string.IsNullOrWhiteSpace(responseText))
            throw new InvalidOperationException(
                "The AI agent could not generate a response.");

        // 7. Store the agent's response
        Console.WriteLine("=== Storing AI Response ===");

        Response newResponse = new Response
        {
            id = Guid.NewGuid(),
            response_to = newMessage.id,
            session_id = sessionId,
            content = responseText,
            source = 2,
            sent_at = DateTime.UtcNow
        };

        _context.Responses.Add(newResponse);

        await _context.SaveChangesAsync();

        // 8. Update session activity
        await _sessionService.updateSession(sessionId);

        Console.WriteLine("=== Web Chat Message Completed ===");

        // 9. Return the response to Angular
        return new ChatMessageResponse
        {
            messageId = newMessage.id,
            responseId = newResponse.id,
            sessionId = sessionId,
            content = responseText,
            sentAt = newResponse.sent_at
        };
    }
}