using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
}
