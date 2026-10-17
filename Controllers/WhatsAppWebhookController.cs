using System.IO;

using Microsoft.AspNetCore.Mvc;
using Karina.Api.Data;
using Karina.Api.Services;

namespace Karina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{

    private readonly IWhatsAppService _whatsappService;

    private readonly IConfiguration _configuration;

    private readonly string _verifyToken;

    public WhatsAppController(KarinaContext context, IConfiguration configuration, IWhatsAppService whatsAppService)
    {
        _configuration = configuration;
        _whatsappService = whatsAppService;
        _verifyToken = _configuration["Meta:VerifyToken"];
    }

    [HttpGet("testwh")]
    public IActionResult Test()
    {
        return Ok(new
        {
           message = "Webhook funcionando" 
        });
    }

    [HttpGet("webhook")]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string verifyToken
    )
    {
        if (mode == "subscribe" && verifyToken == _verifyToken)
        {
            return Ok(challenge);
        }

        return Unauthorized();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> NewMessage()
    {   
        Console.WriteLine("===Webhook POST Reception===");

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        Console.WriteLine(payload);

        Console.WriteLine("===Reception End===");

        try
        {
            await _whatsappService.incomingMessage(payload);
            return Ok();
        } catch(Exception err)
        {
            Console.WriteLine($"Error at .incomingMessage(): {err}");
        }

        return Ok();
    }
}