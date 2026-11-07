using System.Security.Claims;
using Karina.Api.DTOs.Webchat;
using Karina.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karina.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IWebChatService _webChatService;

    public ChatController(IWebChatService webChatService)
    {
        _webChatService = webChatService;
    }

    [HttpPost("messages")]
    public async Task<IActionResult> sendMessage(
        [FromBody] ChatMessageRequest request)
    {
        try
        {
            // Recover authenticated user ID
            string? userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return Unauthorized();

            // Process message
            ChatMessageResponse response =
                await _webChatService.sendMessage(
                    userId,
                    request.content
                );

            return Ok(response);
        }
        catch (ArgumentException error)
        {
            return BadRequest(new { message = error.Message });
        }
        catch (InvalidOperationException error)
        {
            return StatusCode(502, new { message = error.Message });
        }
        catch (Exception error)
        {
            Console.WriteLine($"=== Chat Controller Error: {error} ===");

            return StatusCode(500, new
            {
                message = "An error occurred while processing your message."
            });
        }
    }
}