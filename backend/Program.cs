using Ntc.Domain.Interface;
using Ntc.Database;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<UsuarioService>();   
var app = builder.Build();

app.MapControllers();

app.Run();
