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

builder.Services.AddCors(options =>
{
  options.AddPolicy("KarinaRequests", policy =>
  {
     policy.WithOrigins("http://localhost:4200" /*URL de Railway para web*/).AllowAnyHeader().AllowAnyMethod();
  });
}
);

var app = builder.Build();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("KarinaRequests");
app.UseAuthorization();
app.MapControllers();

/*Ejecutar*/
app.Run();