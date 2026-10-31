using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Karina.Api.Data;
using Karina.Api.Services;


namespace Karina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly KarinaContext _context;

    private readonly IAuthService _authService;

    public AuthController(KarinaContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        Console.WriteLine($"===Register Request Received via web at: {DateTime.UtcNow}===");

        var result = await _authService.handleUserConfirmation(request);

        if (result == true)
        {
            return Ok(new
            {
                message = "Confirmation Succesfully",
                date = DateTime.Now
            });
        }

        return BadRequest(new
        {
            message = "Confirmation Failed",
            date = DateTime.Now
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> SignIn([FromBody] SignIn request)
    {
        Console.WriteLine($"===Login Request Received via web at: {DateTime.UtcNow}===");

        var result = await _authService.userLogin(request);

        if (!result.login || result.user == null)
        {
            return BadRequest(new
            {
                message = "Login Failed",
                date = DateTime.Now
            });

        }

        var claims = new List<Claim>
        {
          new(ClaimTypes.NameIdentifier, result.user.User_Id),
          new(ClaimTypes.Name, result.user.Name),
          new(ClaimTypes.Email, result.user.Email),
          new(ClaimTypes.MobilePhone, result.user.PhoneNumber)
        };

        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            }
        );

        return Ok(new
        {
            message = "Login Succesfull",
            date = DateTime.Now,
            user = result.user
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var user = new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Name = User.FindFirstValue(ClaimTypes.Name),
            Email = User.FindFirstValue(ClaimTypes.Email),
            PhoneNumber = User.FindFirstValue(ClaimTypes.MobilePhone)
        };

        return Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return Ok(new
        {
            message = "Logout Successful",
            date = DateTime.Now
        });
    }
}