using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Karina.Api.Data;

namespace Karina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly KarinaContext _context;

    public TestController(KarinaContext context)
    {
        _context = context;
    }

    [HttpGet("test")]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "Funcionamiento correcto",
            date = DateTime.Now
        });
    }

    [HttpGet("testbd")]
    public async Task<IActionResult> TestConnection()
    {
        var result = await _context.Database.CanConnectAsync();

        return Ok(new
        {
            database = result ? "Conectado" : "No conectado"
        });
    }

    [HttpGet("testusers")]
    public async Task<IActionResult> TestGet()
    {
        var result = await _context.Users.ToListAsync();

        return Ok(new
        {
            database = result
        });
    }
}