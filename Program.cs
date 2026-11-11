using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.EntityFrameworkCore;
using Karina.Api.Data;
using Karina.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Database connection configured: {!string.IsNullOrWhiteSpace(connectionString)}");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión ConnectionStrings:DefaultConnection."
    );
}

builder.Services.AddDbContext<KarinaContext>(options => options.UseNpgsql(connectionString));

//TODO: Agregar builder.Services.AddScoped para los Servicios
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<IUserStatisticsService, UserStatisticsService>();
builder.Services.AddScoped<IWebChatService, WebChatService>();
builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddHostedService<AutomationScheduler>();

builder.Services.AddHttpClient("OpenMeteo", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(options =>
  {
    options.Cookie.Name = "Karina.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
      context.Response.StatusCode = StatusCodes.Status403Forbidden;
      return Task.CompletedTask;
    };

  });

builder.Services.AddCors(options =>
{
  options.AddPolicy("KarinaRequests", policy =>
  {
     policy.WithOrigins("http://localhost:4200", "https://web-production-d9525.up.railway.app")
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
  });
}
);

if (!string.IsNullOrEmpty(port))
{
  builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("KarinaRequests");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

/*Ejecutar*/
app.Run();