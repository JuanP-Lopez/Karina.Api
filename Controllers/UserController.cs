using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Karina.Api.Data;
using Karina.Api.Services;
using Karina.Api.DTOs.User;


namespace Karina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly KarinaContext _context;

    private readonly IAuthService _authService;

    private readonly IUserStatisticsService _userStatisticsService;

    public UserController(KarinaContext context, IAuthService authService, IUserStatisticsService userStatisticsService)
    {
        _context = context;
        _authService = authService;
        _userStatisticsService = userStatisticsService;
    }

    [Authorize]
    [HttpGet("stats")]
    public async Task<IActionResult> UserStatistics()
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var user_id)) return Unauthorized();

        var stats = await _userStatisticsService.getUserStatistics(user_id);

        return Ok(
            stats
        );
    }

    [Authorize]
    [HttpPost("setPreferences")]
    public async Task<IActionResult> SetPreferences([FromBody] UserPreferencesRequest request)
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var user_id)) return Unauthorized();

        Console.WriteLine($"===Preferences Received: {request}===");

        var preferences = await _userStatisticsService.setUserPreferences(user_id, request);

        return Ok(
            preferences
        );
    }

    [Authorize]
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var user_id)) return Unauthorized();

        var preferences = await _userStatisticsService.getUserPreferences(user_id);

        if (preferences == null)
            return NotFound();

        return Ok(preferences);
    }

}