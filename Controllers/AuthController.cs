using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Karina.Api.Data;

namespace Karina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly KarinaContext _context;

    public AuthController(KarinaContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        Console.WriteLine($"===Register Request Received via web at: {DateTime.UtcNow}===");

        string code = request.Code;
        string email = request.Email;
        string phoneNumber = request.PhoneNumber;
        string password = request.Password;

        Console.WriteLine($"===Data Received: {code}, {email}, {phoneNumber}, {password}===");

        return Ok(new
        {
            message = "Funcionamiento correcto",
            date = DateTime.Now
        });
    }
}