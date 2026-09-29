using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;

namespace Ntc.Database;

public class ConnectionContext : DbContext
{
    public DbSet<Usuario> Usuarios {get;set;}
    public DbSet<Aluno> Alunos {get;set;}

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source = database.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}