using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Karina.Api.Data;
using Karina.Api.Models.Whatsapp;

namespace Karina.Api.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly KarinaContext _context;

    private readonly IAuthService _authService;

    private readonly ISessionService _sessionService;

    private readonly ICommunicationService _communicationService;

    public WhatsAppService(KarinaContext context, IAuthService authService, ISessionService sessionService, ICommunicationService communicationService)
    {
        _context = context;
        _authService = authService;
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
            }
            else
            {
                var name = value.Contacts?.FirstOrDefault()?.Profile?.Name;
                var phoneNumber = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Messages?.FirstOrDefault()?.From;
                var phoneNumberId = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Metadata?.PhoneNumberId;
                var wa_id = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Contacts?.FirstOrDefault()?.WaId;
                var message = value?.Messages?.FirstOrDefault()?.Text?.Body;

                var result = await isUserRegistered(payload);

                bool userFoundedOrNot = result.userFoundedOrNot;
                int role = result.role;
                Guid user_id = result.user_id;

                Console.WriteLine($"===.isUserRegistered() return: {userFoundedOrNot}, {role}===");

                string? session_id = "";

                //TODO: Incorporar soporte para usar la misma sesion si aun no expira (pueden durar maximo una hora activa tras ultimo mensaje)
                if (role == 0)
                {
                    Console.WriteLine("User Unregistered");
                    Guid createdId = await _authService.unconfirmedUserRegistration(name, wa_id);
                    //TODO: Agregar el manejo del mensaje y la respuesta, el sistema debe responder y luego pedir registro
                    var newSession = await _sessionService.createSession(name, createdId);
                    string? sessionId = newSession.session_id;
                    await _communicationService.sendMessage(name, phoneNumberId, phoneNumber, sessionId, wa_id, message);
                    return true;
                }

                var activeSession = await _sessionService.searchSession(user_id);

                var isActive = activeSession.isActive;

                if (isActive == true)
                {   
                    Console.WriteLine("===Using Active Session===");
                    session_id = activeSession.session.ToString();
                }
                else
                {
                    Console.WriteLine("===Not An Active Session===");
                    var session = await _sessionService.createSession(name, user_id);
                    session_id = session.session_id;
                }

                switch (role)
                {
                    case 1:
                        Console.WriteLine("User Unconfirmed");
                        //TODO: Agregar el manejo del mensaje y la respuesta, el sistema debe responder y luego pedir registro
                        await _communicationService.sendMessage(name, phoneNumberId, phoneNumber, session_id, wa_id, message);
                        Console.WriteLine($"===Sending Registration Message at: {DateTime.UtcNow:O}===");
                        var resCreateCode = await _authService.createConfirmationCode(wa_id);
                        string code = resCreateCode.Item2;
                        await sendRegistrationMessage(phoneNumber, phoneNumberId, name, code);
                        break;
                    case 2:
                        Console.WriteLine("User Confirmed");
                        //TODO: Agregar el manejo del mensaje y la respuesta, el sistema debe responder
                        Console.WriteLine($"===Handling Message at: {DateTime.UtcNow:O}===");
                        await _communicationService.sendMessage(name, phoneNumberId, phoneNumber, session_id, wa_id, message);
                        break;
                }
            }

            return true;
        }
        catch (Exception incomingMessageError)
        {
            Console.WriteLine($"===Error Handling Incoming Message at {DateTime.UtcNow:O}: {incomingMessageError}===");
            return false;
        }
    }

    //TODO: Modificar busqueda en base a roles en tabla Users
    public async Task<(bool userFoundedOrNot, int role, Guid user_id)> isUserRegistered(string payload)
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
                return (false, 0, Guid.NewGuid());
            }

            Console.WriteLine($"===WA_ID extracted: {wa_id}===");
            Console.WriteLine($"===Extracted Phone Number: {phoneNumber}===");
            Console.WriteLine($"===Extracted Phone Number Id: {phoneNumberId}===");

            try
            {
                var userFound = _context.Users.FirstOrDefault(u => u.wa_id == wa_id);
                if (userFound != null)
                {
                    Console.WriteLine($"===User found by wa_id: {userFound?.name}, {userFound?.wa_id}, {userFound?.registered_at}===");
                    int role = userFound.role;
                    Guid user_id = userFound.user_id;
                    return (false, role, user_id);
                }

                return (false, 0, Guid.NewGuid());

            }
            catch (Exception db_err)
            {
                Console.WriteLine($"Database error: {db_err}");
                return (false, 0, Guid.NewGuid());
            }

        }
        catch (Exception err)
        {
            Console.WriteLine($"===Error: {err}===");
            return (false, 0, Guid.NewGuid());
        }

    }

    /*
    *  @param phoneNumber: es el numero del usuario que envia el mensaje
    *  @param phoneNumberID: es el id del numero del usuario, necesario para armar la URL del endpoint de META a la que se envia
    *   la solicitud
    */
    public async Task sendRegistrationMessage(string phoneNumber, string phoneNumberId, string name, string code)
    {
        Console.WriteLine("===Trying To Send Registration Message===");

        string url = $" https://graph.facebook.com/v25.0/{phoneNumberId}/messages";

        string webUrl = "http://localhost:4200/";

        string accessToken = "EAAPttAPoJgcBSztaxC9bjstbSw1S1TcFv7340E1Q1XWTQtzFduoZCtNpriUXHXrvJafzA9Sun0RNPWZB0VVJUdRMjLG27kqMHAh76VBVdwV81lq4j9huDZCqQRwTBZAczRw44922IAqqLLOfUKahq2Tme2NpE949BiNPtUHo6rGG1Wsna8553cqeZAZC8GaEEZARhwup51uOZCm2ZBwiJ3nWzpohvDWpDT9to3gvLCJWFz4riT60ZCarOxwZABs1poLD603ibsFTVoZAZCitUn1Hi6Igx7v0ZD";

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
                    body = $"¡Hola, {name}! \n\nParece que no estás registrado; para registrarte, continua con el siguiente enlace: {webUrl}.\nIngresa a crear cuenta y usa el siguiente codigo: {code}"
                }
            };

            string payload_to_json = JsonSerializer.Serialize(payload);
            var content = new StringContent(payload_to_json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string response_to_string = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"===Registration Message Sended at: {DateTime.UtcNow:O}===");
            }
            else
            {
                Console.WriteLine($"===Error Sending Registration Message: ({response.StatusCode}) : {response_to_string} ===");
            }
        }
    }

}