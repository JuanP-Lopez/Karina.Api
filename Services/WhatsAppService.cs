using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Karina.Api.Data;
using Karina.Api.Models.Whatsapp;

namespace Karina.Api.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly KarinaContext _context;

    private readonly IUserService _userService;

    private readonly ISessionService _sessionService;

    private readonly ICommunicationService _communicationService;

    public WhatsAppService(KarinaContext context, IUserService userService, ISessionService sessionService, ICommunicationService communicationService)
    {
        _context = context;
        _userService = userService;
        _sessionService = sessionService;
        _communicationService = communicationService;
    }

    public async Task<bool> incomingMessage(string payload)
    { 
        var data = JsonSerializer.Deserialize<WhatsAppWebhookModel>(payload);

        try
        {
            var value = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value;
            if (value?.Messages is null || value.Messages.Count == 0)
            {
                Console.WriteLine($"===Notification Ignored, Not a Message: {DateTime.UtcNow:O}===");
                return false;
            } else
            {
                var name = value.Contacts?.FirstOrDefault()?.Profile?.Name;
                var result = await isUserUnconfirmed(payload);

                bool isUserConfirmedOrNot = result.isUserConfirmedOrNot;
                string? phoneNumber = result.phoneNumber;
                string? phoneNumberId = result.phoneNumberId;
                string? wa_id = result.wa_id;

                Console.WriteLine($"===.isUserConfirmed() return: {isUserConfirmedOrNot}, {phoneNumber}, {phoneNumberId}===");

                switch (isUserConfirmedOrNot)
                {
                    case true:
                        Console.WriteLine("User Confirmed");
                        //TODO: Agregar el manejo del mensaje y la respuesta, el sistema debe responder
                        Console.WriteLine($"===Handling Message at: {DateTime.UtcNow:O}===");
                        
                        break;
                    case false:
                        Console.WriteLine("User Unconfirmed");
                        //TODO: Agregar el manejo del mensaje y la respuesta, el sistema debe responder y luego pedir registro
                        var session = await _sessionService.createSession("prueba");
                        
                        string? session_id = session.session_id;
                        await _communicationService.sendMessage(phoneNumberId, phoneNumber, session_id);
                        // Console.WriteLine($"===Sending Registration Message at: {DateTime.UtcNow:O}===");
                        // await _userService.unconfirmedUserRegistration(name, wa_id);
                        // await sendRegistrationMessage(phoneNumber, phoneNumberId, name);
                        break;
                }
            }

            return true;
        } catch (Exception incomingMessageError)
        {
            Console.WriteLine($"===Error Handling Incoming Message at {DateTime.UtcNow:O}: {incomingMessageError}===");
            return false;
        }
    }

    public async Task<(bool isUserConfirmedOrNot, string? phoneNumber, string? phoneNumberId, string? wa_id)> isUserUnconfirmed(string payload)
    {

        Console.WriteLine("===Reading payload to extract wa_id===");
        var data = JsonSerializer.Deserialize<WhatsAppWebhookModel>(payload);
        try
        {
            var phoneNumber = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Messages?.FirstOrDefault()?.From;
            var phoneNumberId = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Metadata?.PhoneNumberId;
            var wa_id = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Contacts?.FirstOrDefault()?.WaId;

            if (string.IsNullOrWhiteSpace(wa_id))
            {   
                Console.WriteLine("===WA_ID not found===");
                return (false, phoneNumber, phoneNumberId, wa_id);
            }

            Console.WriteLine($"===WA_ID extracted: {wa_id}===");
            Console.WriteLine($"===Extracted Phone Number: {phoneNumber}===");
            Console.WriteLine($"===Extracted Phone Number Id: {phoneNumberId}===");

            try
            {
                var userFoundUnconfirmed = _context.UnconfirmedUsers.FirstOrDefault(u => u.wa_id == wa_id);
                if (userFoundUnconfirmed != null)
                {
                    Console.WriteLine($"===Unconfirmed user found by wa_id: {userFoundUnconfirmed?.name}, {userFoundUnconfirmed?.wa_id}, {userFoundUnconfirmed?.registered_at}===");
                    return (false, phoneNumber, phoneNumberId, wa_id);
                }

                var userFoundConfirmed = _context.Users.FirstOrDefault(u => u.wa_id == wa_id);
                if (userFoundConfirmed != null)
                {
                    Console.WriteLine($"===Confirmed user found by wa_id: {userFoundConfirmed?.name}, {userFoundConfirmed?.wa_id}, {userFoundConfirmed?.registered_at}===");
                    return (true, phoneNumber, phoneNumberId, wa_id);
                }
                
                return (false, phoneNumber, phoneNumberId, wa_id);

            } catch(Exception db_err)
            {
                Console.WriteLine($"Database error: {db_err}");
                return (false, phoneNumber, phoneNumberId, wa_id);
            }
             
        } catch (Exception err)
        {
            Console.WriteLine($"===Error: {err}===");
            return (false, "phoneNumber", "phoneNumberId", "wawa");
        }

    }

    /*
    *  @param phoneNumber: es el numero del usuario que envia el mensaje
    *  @param phoneNumberID: es el id del numero del usuario, necesario para armar la URL del endpoint de META a la que se envia
    *   la solicitud
    */
    public async Task sendRegistrationMessage(string phoneNumber, string phoneNumberId, string name)
    {   
        Console.WriteLine("===Trying To Send Registration Message===");

        string url = $" https://graph.facebook.com/v25.0/{phoneNumberId}/messages";

        string webUrl = "http://localhost:4200/";

        string accessToken = "EAAPttAPoJgcBSpamUIW1ZCuLDQARBPkJhvr63jCyOwjq9ZAu3TnceZCYLHp3HJ8ZCMySdZBBmraZCseY5eVSZCKcIpXWPIYdRPZCi4NWXrpPKY7nPJGN8FeDbcX4FJe0OeM28z0FZCdbZBf4fc9DZB6lFkkhAG5c4LrGZACgMrCoXSrZBn1pTFTxtcjwJJ5P9ZCZC3dkI6BPKy7Yux1xVPBrN4MT6pCLkeb9ZANZCoPsxvBZBaAXITTfGnJikQrVnz8Ij83I85Ei8PM4mzI4nz8oszt6y44ZAky";

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
                    body = $"¡Hola, {name}! \n\nParece que no estás registrado; para registrarte, continua con el siguiente enlace: {webUrl}"
                }
            };

            string payload_to_json = JsonSerializer.Serialize(payload);
            var content = new StringContent(payload_to_json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string response_to_string = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"===Registration Message Sended at: {DateTime.UtcNow:O}===");
            } else
            {
                Console.WriteLine($"===Error Sending Registration Message: ({response.StatusCode}) : {response_to_string} ===");
            }
        }
    }

    public async Task<bool> handleConfirmedUser() 
    {
        return true;
    }
    public async Task sendMessageToAgent(string message)
    {
        
    }
}