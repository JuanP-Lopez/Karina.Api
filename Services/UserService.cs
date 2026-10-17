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

    public async Task<bool> unconfirmedUserRegistration(string receivedUser, string receiverWa_id)
    {   
        try
        {
            if(string.IsNullOrWhiteSpace(receivedUser) || string.IsNullOrWhiteSpace(receiverWa_id))
            {
                return false;
            }

            Console.WriteLine($"===Received Attributes: {receivedUser}, {receiverWa_id}===");

            UnconfirmedUser user = new UnconfirmedUser
            {
              name = receivedUser,
              wa_id = receiverWa_id  
            };
            
            _context.UnconfirmedUsers.Add(user);
            await _context.SaveChangesAsync();
            return true;
        } catch (Exception unconfirmedError)
        {
            Console.WriteLine($"===Error At Unconfirmed User Registration: {unconfirmedError}===");
            return false;
        }
    }
}