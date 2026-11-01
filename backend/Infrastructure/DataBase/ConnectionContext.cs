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
    public DbSet<Escola> Escolas {get;set;}
    public DbSet<UnidadeEscolar> Unidades {get;set;}
    public DbSet<VinculoResponsavelAluno> Vinculos {get;set;}
    public DbSet<ConsumoBootstrap> ConsumosBootstrap {get;set;}
    public DbSet<TokenDeCadastro> TokensDeCadastro {get;set;}

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

        // A mesma unidade só pode consumir o token de ambiente uma vez (Spec 022 v2).
        modelBuilder.Entity<ConsumoBootstrap>()
            .HasIndex(c => c.UnidadeId)
            .IsUnique();

        modelBuilder.Entity<ConsumoBootstrap>()
            .HasOne<UnidadeEscolar>()
            .WithMany()
            .HasForeignKey(c => c.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Um hash por token: o valor em claro nunca é persistido.
        modelBuilder.Entity<TokenDeCadastro>()
            .HasIndex(t => t.Hash)
            .IsUnique();

        // Consulta do painel: token da unidade ainda não expirado.
        modelBuilder.Entity<TokenDeCadastro>()
            .HasIndex(t => new { t.UnidadeId, t.ExpiraEm });

        modelBuilder.Entity<TokenDeCadastro>()
            .HasOne<UnidadeEscolar>()
            .WithMany()
            .HasForeignKey(t => t.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TokenDeCadastro>()
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Usuários anteriores à Spec 009 são promovidos a ativos pela migration; o
        // papel fica nulo de propósito e só autentica depois de um admin defini-lo.
        modelBuilder.Entity<Usuario>()
            .Property(u => u.Ativo)
            .HasDefaultValue(true);

        modelBuilder.Entity<Usuario>()
            .HasOne<Escola>()
            .WithMany()
            .HasForeignKey(u => u.EscolaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Usuario>()
            .HasOne<UnidadeEscolar>()
            .WithMany()
            .HasForeignKey(u => u.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Nome único sem diferenciar caixa: NOCASE no índice resolve a comparação no
        // banco, e o serviço ainda checa antes de gravar para devolver 409 com mensagem.
        modelBuilder.Entity<Escola>()
            .Property(e => e.Nome)
            .UseCollation("NOCASE");

        modelBuilder.Entity<Escola>()
            .HasIndex(e => e.Nome)
            .IsUnique();

        modelBuilder.Entity<UnidadeEscolar>()
            .Property(u => u.Nome)
            .UseCollation("NOCASE");

        // Nome único na rede: a unidade é organizacional, não pertence a uma escola.
        modelBuilder.Entity<UnidadeEscolar>()
            .HasIndex(u => u.Nome)
            .IsUnique();

        // Escola de origem é opcional (dado legado) e não define mais a existência da unidade.
        modelBuilder.Entity<UnidadeEscolar>()
            .HasOne<Escola>()
            .WithMany()
            .HasForeignKey(u => u.EscolaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Aluno>()
            .HasOne<UnidadeEscolar>()
            .WithMany()
            .HasForeignKey(a => a.UnidadeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VinculoResponsavelAluno>()
            .HasKey(v => new { v.AlunoId, v.ResponsavelUsuarioId });

        modelBuilder.Entity<VinculoResponsavelAluno>()
            .HasOne<Aluno>()
            .WithMany()
            .HasForeignKey(v => v.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VinculoResponsavelAluno>()
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(v => v.ResponsavelUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

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