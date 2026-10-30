using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using Ntc.Database;

namespace Backend.Tests.Integration;

/// <summary>
/// Sobe a API real com SQLite em memória. A conexão é aberta antes do host iniciar
/// (o startup chama <c>Database.Migrate()</c>) e permanece aberta durante toda a
/// vida da fábrica, porque SQLite em memória é descartado ao fechar a conexão.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ConnectionContext>>();
            services.RemoveAll<ConnectionContext>();
            services.AddDbContext<ConnectionContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }

    /// <summary>
    /// Cria um cliente com cookie de sessão válido. Registra um usuário único por
    /// chamada e faz login com <c>HandleCookies = true</c>.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var email = $"aluno.api.{Guid.NewGuid():N}@nutrieduc.local";

        var registro = await client.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Usuario de Teste",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email,
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null
        });
        registro.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/usuario/login", new
        {
            email,
            senha = "senha12345"
        });
        login.EnsureSuccessStatusCode();

        return client;
    }
}
