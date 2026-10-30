using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;

namespace Ntc.Database;

public class ConnectionContext : DbContext
{
    public ConnectionContext()
    {
    }

    public ConnectionContext(DbContextOptions<ConnectionContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios {get;set;}
    public DbSet<Aluno> Alunos {get;set;}

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (options.IsConfigured)
            return;

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

        // Turma e Responsavel são objetos de valor do Aluno: não têm identidade nem
        // chave própria, por isso são mapeados como tipo complexo (e não como entidade
        // possuída, que criaria uma coluna de chave AlunoId inexistente). Ficam nas
        // colunas que já existiam — Escola, Serie, NomeResponsavel, TelefoneResponsavel —
        // portanto sem mudança de schema e sem migration nova.
        modelBuilder.Entity<Aluno>()
            .ComplexProperty(a => a.Turma, turma =>
            {
                turma.Property(t => t.Escola)
                    .HasColumnName("Escola")
                    .IsRequired();

                turma.Property(t => t.Serie)
                    .HasColumnName("Serie")
                    .IsRequired();
            });

        modelBuilder.Entity<Aluno>()
            .ComplexProperty(a => a.Responsavel, responsavel =>
            {
                responsavel.Property(r => r.Nome)
                    .HasColumnName("NomeResponsavel")
                    .IsRequired();

                responsavel.Property(r => r.Telefone)
                    .HasColumnName("TelefoneResponsavel")
                    .IsRequired();
            });
    }
}