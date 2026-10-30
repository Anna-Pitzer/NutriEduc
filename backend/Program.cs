using Microsoft.AspNetCore.Authentication.Cookies;
using Ntc.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using Ntc.Database;
using Ntc.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5001");

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source = database.db";

builder.Services.AddControllers();
builder.Services.AddContratoDeErro();

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();

builder.Services.AddScoped<AlunoService>();
builder.Services.AddScoped<IAlunoRepository, AlunoRepository>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "NutriEduc.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { erro = "Sessão inválida ou ausente." });
        };

        options.Events.OnRedirectToAccessDenied = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { erro = "Acesso negado." });
        };
    });



builder.Services.AddAuthorization();
builder.Services.AddDbContext<ConnectionContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConnectionContext>();
    db.Database.Migrate();
}

app.UseContratoDeErro();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/api/diagnostico/erro",
        () => Task.FromException(new Exception("Falha de teste.")));
    app.MapGet("/api/diagnostico/conflito",
        () => Task.FromException(new InvalidOperationException("Conflito de teste.")));
    app.MapGet("/api/diagnostico/inexistente",
        () => Task.FromException(new KeyNotFoundException("Não encontrado.")));
}

app.Run();

/// <summary>
/// Exposta para que o projeto de testes possa referenciar a aplicação (Spec 005, T2).
/// O compilador já gera esta classe a partir das instruções de nível superior; o
/// <c>partial</c> apenas amplia a visibilidade de <c>internal</c> para <c>public</c>.
/// </summary>
public partial class Program
{
}