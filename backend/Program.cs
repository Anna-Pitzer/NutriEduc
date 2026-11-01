using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ntc.Database;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Middleware;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5001");

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source = database.db";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));
builder.Services.AddContratoDeErro();

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<EscopoService>();
builder.Services.AddScoped<EscolaService>();
builder.Services.AddScoped<VinculoService>();
builder.Services.AddScoped<BootstrapService>();
builder.Services.AddScoped<ExclusaoService>();
builder.Services.AddScoped<IConsumoBootstrapRepository, ConsumoBootstrapRepository>();
builder.Services.AddScoped<ITokenDeCadastroRepository, TokenDeCadastroRepository>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IEscolaRepository, EscolaRepository>();
builder.Services.AddScoped<IVinculoRepository, VinculoRepository>();

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

        // Único ponto que invalida sessões (RF5): usuário desativado ou removido perde
        // a sessão já emitida na requisição seguinte, sem lógica duplicada por endpoint.
        options.Events.OnValidatePrincipal = async context =>
        {
            var repositorio = context.HttpContext.RequestServices
                .GetRequiredService<IUsuarioRepository>();

            var identificador = context.Principal?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(identificador, out var usuarioId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var usuario = repositorio.GetById(usuarioId);

            if (usuario is null || !usuario.Ativo || usuario.Papel is null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SessaoValida", politica => politica.RequireAuthenticatedUser());

    options.AddPolicy("AdminSecretaria", politica =>
        politica.RequireRole(nameof(PapelUsuario.AdminSecretaria)));

    options.AddPolicy("AdminEscola", politica =>
        politica.RequireRole(nameof(PapelUsuario.AdminEscola)));

    // Administração de usuários e vínculos: secretaria (global) ou escola (escopo).
    options.AddPolicy("Admin", politica =>
        politica.RequireRole(
            nameof(PapelUsuario.AdminSecretaria),
            nameof(PapelUsuario.AdminEscola)));

    // Leitura da equipe escolar. AdminSecretaria entra na lista porque é global e não
    // deve ser barrada por uma política de leitura — a diferença é o escopo de dados.
    options.AddPolicy("EquipeEscolar", politica =>
        politica.RequireRole(
            nameof(PapelUsuario.Nutricionista),
            nameof(PapelUsuario.Merendeira),
            nameof(PapelUsuario.AdminEscola),
            nameof(PapelUsuario.AdminSecretaria)));

    // Leitura de aluno: todos os papéis leem, mas cada um vê apenas o próprio escopo
    // (RF8 e RF9). A restrição real está no filtro do repositório.
    options.AddPolicy("LeituraAluno", politica =>
        politica.RequireRole(
            nameof(PapelUsuario.Nutricionista),
            nameof(PapelUsuario.Merendeira),
            nameof(PapelUsuario.Responsavel),
            nameof(PapelUsuario.AdminEscola),
            nameof(PapelUsuario.AdminSecretaria)));

    // Escrita de aluno: merendeira é somente leitura e responsável não administra.
    options.AddPolicy("GestaoAluno", politica =>
        politica.RequireRole(
            nameof(PapelUsuario.Nutricionista),
            nameof(PapelUsuario.AdminEscola),
            nameof(PapelUsuario.AdminSecretaria)));
});

builder.Services.AddDbContext<ConnectionContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConnectionContext>();
    db.Database.Migrate();

    CriarAdminInicial(scope.ServiceProvider, builder.Configuration, app.Logger);
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
/// Seed idempotente do primeiro AdminSecretaria (Spec 009, T11). Sem configuração, o
/// startup apenas registra aviso: nenhuma senha vive no código ou no repositório.
/// </summary>
static void CriarAdminInicial(
    IServiceProvider servicos,
    IConfiguration configuracao,
    ILogger logger)
{
    var email = configuracao["AdminInicial:Email"];
    var senha = configuracao["AdminInicial:Senha"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
    {
        logger.LogWarning(
            "AdminInicial:Email/Senha não configurados; nenhum administrador inicial foi criado.");
        return;
    }

    var usuarios = servicos.GetRequiredService<IUsuarioRepository>();

    if (usuarios.GetByEmail(email) is not null)
    {
        return;
    }

    var admin = new Usuario(
        "Administrador Inicial",
        "00000000000",
        new DateOnly(1990, 1, 1),
        email,
        PapelUsuario.AdminSecretaria);

    admin.DefinirSenha(senha);
    usuarios.Add(admin);

    logger.LogInformation("Administrador inicial criado para {Email}.", email);
}

/// <summary>
/// Exposta para que o projeto de testes possa referenciar a aplicação (Spec 005, T2).
/// O compilador já gera esta classe a partir das instruções de nível superior; o
/// <c>partial</c> apenas amplia a visibilidade de <c>internal</c> para <c>public</c>.
/// </summary>
public partial class Program
{
}
