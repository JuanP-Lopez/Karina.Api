using Karina.Api.Data;
using Karina.Api.Models;

namespace Karina.Api.Services;

public class UserService : IUserService
{
    private readonly KarinaContext _context;

    public UserService(KarinaContext context)
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
            if(string.IsNullOrWhiteSpace(receivedUser) || string.IsNullOrWhiteSpace(receivedWa_id))
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
        } catch (Exception userError)
        {
            Console.WriteLine($"===Error At Unconfirmed User Registration: {userError}===");
            return false;
        }
    }
}