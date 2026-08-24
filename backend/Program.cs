using Ntc.Controllers;
using Microsoft.AspNetCore.Builder;
using Ntc.Model;
using Ntc.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();   
var app = builder.Build();

app.MapControllers();

app.Run();
