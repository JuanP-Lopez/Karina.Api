using System;
using System.Linq;

using Karina.Api.Data;
using Karina.Api.Models;

namespace Karina.Api.Services;

public class AuthService : IAuthService
{
    private readonly KarinaContext _context;

    private readonly string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public AuthService(KarinaContext context)
    {
        _context = context;
    }

    public async Task<bool> handleUserConfirmation()
    {
        return true;
    }

    //TODO: Modificar registro para basarlo en roles
    public async Task<bool> unconfirmedUserRegistration(string receivedUser, string receivedWa_id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(receivedUser) || string.IsNullOrWhiteSpace(receivedWa_id))
            {
                return false;
            }

            Console.WriteLine($"===Received Attributes: {receivedUser}, {receivedWa_id}===");

            User user = new User
            {
                name = receivedUser,
                wa_id = receivedWa_id,
                role = 1
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception userError)
        {
            Console.WriteLine($"===Error At Unconfirmed User Registration: {userError}===");
            return false;
        }
    }

    public async Task<bool> createConfirmationCode(Guid user_id, string wa_id)
    {
        Random rand = new Random();
        int lenght = 6;

        string randomCode = new string(Enumerable.Repeat(characters, lenght)
            .Select(s => s[rand.Next(s.Length)]).ToArray());
        Console.WriteLine($"===Random Access Code: {randomCode}===");

        RandomCode code = new RandomCode
        {
          code = randomCode,
          user_id = user_id,
          wa_id = wa_id
        };

        _context.

        return false;
    }
}