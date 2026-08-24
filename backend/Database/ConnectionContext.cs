using Microsoft.EntityFrameworkCore;
using Ntc.Model;

namespace Ntc.Database;

public class ConnectionContext : DbContext
{
    public DbSet<Usuario> Usuarios {get;set;}

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source = database.db");
    }
}