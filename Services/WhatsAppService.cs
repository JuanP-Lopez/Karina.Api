// using System;
using System.Text;
using System.Text.Json;
// using System.Net.Http;
using System.Net.Http.Headers;
// using System.Text.Json.Serialization;

// using Microsoft.EntityFrameworkCore;
using Karina.Api.Data;
// using Karina.Api.Models;
using Karina.Api.Models.Whatsapp;

namespace Karina.Api.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly KarinaContext _context;

    public WhatsAppService(KarinaContext context)
    {
        _context = context;
    }

    public async Task incomingMessage(string payload)
    {

        //TODO: Funcion orquestadora incomingMessage() debe validar si es un mensaje o notificacion
        
        //TODO: Realizar validacion de registro o no registro, el flujo depende de eso
        var isRegistered = await isUserRegistered(payload);
        if(!isRegistered)
        {
            return;
        }
    }

    public async Task<bool> isUserRegistered(string payload)
    {

        Console.WriteLine("===Reading payload to extract wa_id===");

        try
        {
            var data = JsonSerializer.Deserialize<WhatsAppWebhookModel>(payload);

            //TODO: Este fragmento comprueba si se trata de un mensaje o notificacion debe moverse a incomingMessage()
            var value = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value;
            if (value?.Messages is null || value.Messages.Count == 0)
            {
                Console.WriteLine($"===Notification Ignored Not a Message: {DateTime.UtcNow:O}===");
                return false;
            }
            //TODO: END

            var phoneNumber = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Messages?.FirstOrDefault()?.From;
            var phoneNumberId = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Metadata?.PhoneNumberId;
            var wa_id = data?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Contacts?.FirstOrDefault()?.WaId;

            if (string.IsNullOrWhiteSpace(wa_id))
            {   
                Console.WriteLine("===WA_ID not found===");
                return false;
            }

            Console.WriteLine($"===WA_ID extracted: {wa_id}===");
            Console.WriteLine($"===Extracted Phone Number: {phoneNumber}===");
            Console.WriteLine($"===Extracted Phone Number Id: {phoneNumberId}===");

            try
            {
                var userFound = _context.Users.FirstOrDefault(u => u.wa_id == wa_id);
                Console.WriteLine($"===User found by wa_id: {userFound?.name}, {userFound?.email}, {userFound?.phone_number}===");

                //TODO: Mover envio de mensaje de registro a incomingMessage() cuando el usuario no este registrado
                if (userFound == null)
                {   
                    Console.WriteLine($"===User Not Found Sending Registration Message at: {DateTime.UtcNow:O}===");
                    await sendRegistrationMessage(phoneNumber, phoneNumberId);
                    return false;
                }
                //TODO: END

                //! Modificar retorno para regresar valores necesarios para enviar mensaje de registro (probar metodo de tuplas)
                return true;

            } catch(Exception db_err)
            {
                Console.WriteLine($"Database error: {db_err}");
                return false;
            }
             
        } catch (Exception err)
        {
            Console.WriteLine($"===Error: {err}===");
            return false;
        }

    }

    /*
    *  @param phoneNumber: es el numero del usuario que envia el mensaje
    *  @param phoneNumberID: es el id del numero del usuario, necesario para armar la URL del endpoint de META a la que se envia
    *   la solicitud
    */
    public static async Task sendRegistrationMessage(string phoneNumber, string phoneNumberId)
    {
        string url = $" https://graph.facebook.com/v25.0/{phoneNumberId}/messages";

        string accessToken = "EAAPttAPoJgcBSt0IuwV6XZCTxlLmuCLkg1OeRsVxR5FbNu0yZB36lavuZCQsI8OpfxR1f75NtwyQ99rNuYjiSBXsfUi4SPsZBXsR9U1Is3yC08Ao0yjOCkw5li5X8gvS90BXmumiDH5dPZBUZCniPo0j04nCkTLts2RVVCmaP0Ao6jIUhkoojxoadQGnHkZARHKGWBKf5dgIZCX695ZCLlF1w25ZCHLDBfJ49SUlMfKNRJxfkZCsdeyFXGDV4OGAZCPZCswvqyeKaJJFQTFLHjIwiSTmB";

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
                    body = "¡Hola, estimado posible usuario! \n\nParece que no estás registrado, para registrarte, continua con el siguiente enlace:"
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
}