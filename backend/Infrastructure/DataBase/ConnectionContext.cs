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

        // Restricoes é IReadOnlyList<string> sobre o campo privado _restricoes.
        // Sem esta configuração o EF Core não materializa a coleção, porque a
        // propriedade é somente leitura e não tem setter. HasField aponta o campo
        // de apoio e PropertyAccessMode.Field faz o EF popular o campo direto.
        // Renomear _restricoes exige atualizar esta linha (Spec 003).
        modelBuilder.Entity<Aluno>()
            .PrimitiveCollection(a => a.Restricoes)
            .HasField("_restricoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}