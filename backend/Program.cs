using Ntc.Controllers;
using Microsoft.AspNetCore.Builder;
using Ntc.Domain;
using Ntc.Database;
using Ntc.Domain.Entity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<UsuarioService>();   
var app = builder.Build();

app.MapControllers();

app.Run();
