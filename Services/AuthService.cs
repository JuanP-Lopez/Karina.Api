using System;
using System.Linq;

using Karina.Api.Data;
using Karina.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Karina.Api.Services;

public class AuthService : IAuthService
{
    private readonly KarinaContext _context;

    private readonly string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public AuthService(KarinaContext context)
    {
        _context = context;
    }

    public async Task<(bool login, AuthResponse? user)> userLogin(SignIn request)
    {

        try
        {   
            AuthResponse response = new AuthResponse
            {
            
            };

            string? email = request.Email;
            string? password = request.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return (false, response);

            var userFounded = await _context.Users.Where(u => u.email == email && u.password == password).FirstOrDefaultAsync();

            if (userFounded != null)
            {
                response.User_Id = userFounded.user_id.ToString();
                response.Name = userFounded.name;
                response.Email = userFounded.email;
                response.PhoneNumber= userFounded.phone_number;

                Console.WriteLine($"===User Founded: {response}===");

                return (true, response);
            }
            
            return (false, response);
        }
        catch (Exception loginError)
        {
            AuthResponse response = new AuthResponse
            {

            };

            Console.WriteLine($"===Error at Login: {loginError}===");

            return (false, response);
        }
    }

    public async Task<bool> handleUserConfirmation(SignUpRequest request)
    {
        try
        {

            string code = request.Code;
            string email = request.Email;
            string phoneNumber = request.PhoneNumber;
            string password = request.Password;

            Console.WriteLine($"===Data Received: {code}, {email}, {phoneNumber}, {password}===");

            var existingCode = await _context.Codes.Where(u => u.code == code).FirstOrDefaultAsync();

            if (existingCode == null) return false;

            Guid user_id = existingCode.user_id;

            await _context.Users.Where(u => u.user_id == user_id)
                .ExecuteUpdateAsync(
                    s => s
                    .SetProperty(p => p.email, email)
                    .SetProperty(p => p.phone_number, phoneNumber)
                    .SetProperty(p => p.password, password)
                    .SetProperty(p => p.role, 2)
                );

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception confirmationError)
        {
            Console.WriteLine($"===Confirmation Failed: {confirmationError}===");
            return false;
        }
    }

    //TODO: Modificar registro para basarlo en roles
    public async Task<Guid> unconfirmedUserRegistration(string receivedUser, string receivedWa_id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(receivedUser) || string.IsNullOrWhiteSpace(receivedWa_id))
            {
                return Guid.Empty;
            }

            Console.WriteLine($"===Received Attributes: {receivedUser}, {receivedWa_id}===");

            Guid userGuid = Guid.NewGuid();

            User user = new User
            {
                user_id = userGuid,
                name = receivedUser,
                registered_at = DateTime.UtcNow,
                wa_id = receivedWa_id,
                role = 1
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return userGuid;
        }
        catch (Exception userError)
        {
            Console.WriteLine($"===Error At Unconfirmed User Registration: {userError}===");
            return Guid.Empty;
        }
    }

    public async Task<(bool, string)> createConfirmationCode(string wa_id)
    {

        try
        {

            var userId = await _context.Users.Where(u => u.wa_id == wa_id).FirstOrDefaultAsync();
            Guid user_id = userId.user_id;

            Console.WriteLine("===Searching If a Confirmation Code is Available===");
            var searchCode = await _context.Codes.Where(u => u.user_id == user_id && u.wa_id == wa_id).FirstOrDefaultAsync();

            var confirmationCode = searchCode?.code;

            if (!String.IsNullOrWhiteSpace(confirmationCode))
            {
                Console.WriteLine($"Confirmation Code Founded {confirmationCode} For User {user_id}");
                return (false, confirmationCode);
            }
            else
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

                _context.Codes.Add(code);
                await _context.SaveChangesAsync();

                return (true, randomCode);
            }

        }
        catch (Exception codeError)
        {
            Console.WriteLine($"===Error Generating Confirmation Code: {codeError}===");
            return (false, "");
        }
    }
}