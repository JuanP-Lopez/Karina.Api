using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.EntityFrameworkCore;
using Karina.Api.Data;
using Karina.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<KarinaContext>(options => options.UseNpgsql(connectionString));

//TODO: Agregar builder.Services.AddScoped para los Servicios
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(options =>
  {
    options.Cookie.Name = "Karina.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

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
     policy.WithOrigins("http://localhost:4200" /*URL de Railway para web*/)
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
  });
}
);

var app = builder.Build();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("KarinaRequests");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

/*Ejecutar*/
app.Run();