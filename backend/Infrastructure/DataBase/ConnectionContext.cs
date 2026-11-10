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
    public DbSet<RestricaoAlimentar> RestricoesAlimentares {get;set;}
    public DbSet<HistoricoAluno> HistoricosAlunos {get;set;}
    public DbSet<CategoriaAlimento> CategoriasAlimento {get;set;}
    public DbSet<Alimento> Alimentos {get;set;}
    public DbSet<AlimentoRestricao> AlimentosRestricoes {get;set;}
    public DbSet<Cardapio> Cardapios {get;set;}
    public DbSet<Refeicao> Refeicoes {get;set;}
    public DbSet<ItemRefeicao> ItensRefeicao {get;set;}
    public DbSet<ParametroGeracao> ParametrosGeracao {get;set;}
    public DbSet<HistoricoCardapio> HistoricosCardapios {get;set;}
    public DbSet<SubstituicaoDeItem> SubstituicoesDeItem {get;set;}
    public DbSet<PendenciaAdaptacao> PendenciasAdaptacao {get;set;}
    public DbSet<ConfirmacaoDeIncompatibilidade> ConfirmacoesDeIncompatibilidade {get;set;}
    public DbSet<Solicitacao> Solicitacoes {get;set;}
    public DbSet<HistoricoSolicitacao> HistoricoSolicitacoes {get;set;}
    public DbSet<SolicitacaoAlteracao> SolicitacaoAlteracoes {get;set;}
    public DbSet<Notificacao> Notificacoes {get;set;}
    public DbSet<TrilhaAcao> TrilhaAcoes {get;set;}
    public DbSet<Ocorrencia> Ocorrencias {get;set;}
    public DbSet<HistoricoOcorrencia> HistoricoOcorrencias {get;set;}

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

        // O hash da senha é interno à entidade (Spec 024, F8); a coluna continua "Senha"
        // para preservar o schema. Sem esta configuração o EF não descobriria a propriedade
        // não pública e o valor não seria persistido.
        modelBuilder.Entity<Usuario>()
            .Property<string>(nameof(Usuario.Senha))
            .HasColumnName("Senha");

        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.Crn)
            .HasMaxLength(20);

        // CRN único entre usuários (Spec 031, RNF3). O índice filtrado permite vários
        // valores nulos, mas nunca dois registros com o mesmo CRN normalizado.
        modelBuilder.Entity<Usuario>()
            .HasIndex(usuario => usuario.Crn)
            .IsUnique()
            .HasFilter("\"Crn\" IS NOT NULL");

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
            .HasOne<UnidadeEscolar>()
            .WithMany()
            .HasForeignKey(e => e.UnidadeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Nome único por unidade (Spec 027); o filtro preserva escolas legadas sem unidade.
        modelBuilder.Entity<Escola>()
            .HasIndex(e => new { e.UnidadeId, e.Nome })
            .IsUnique()
            .HasFilter("\"UnidadeId\" IS NOT NULL");

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

        // Escola é a fonte de verdade do vínculo institucional (Spec 027).
        modelBuilder.Entity<Aluno>()
            .HasOne<Escola>()
            .WithMany()
            .HasForeignKey(a => a.EscolaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Aluno>()
            .HasIndex(a => a.EscolaId);

        // Unicidade do catálogo por categoria: a coluna normalizada resolve caixa e acento,
        // que o SQLite não compara nativamente (Spec 010, RF1).
        modelBuilder.Entity<RestricaoAlimentar>()
            .HasIndex(r => new { r.NomeNormalizado, r.Categoria })
            .IsUnique();

        // Histórico é coleção do agregado Aluno: o EF preenche AlunoId ao gravar o grafo,
        // inclusive quando o aluno é novo e o id ainda não existe (Spec 010, RNF2).
        modelBuilder.Entity<Aluno>()
            .HasMany(a => a.Historico)
            .WithOne()
            .HasForeignKey(h => h.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Leitura paginada sempre por aluno e data decrescente (Spec 023, RF6, ADR-009).
        modelBuilder.Entity<HistoricoAluno>()
            .HasIndex(h => new { h.AlunoId, h.CriadoEmUtc });

        modelBuilder.Entity<HistoricoAluno>()
            .Property(historico => historico.Origem)
            .HasMaxLength(100);

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

        // Nome único por catálogo, sem diferenciar caixa nem acento: a coluna normalizada
        // carrega o índice único, porque o SQLite não compara assim nativamente
        // (Spec 011, RF1 e RF2).
        modelBuilder.Entity<CategoriaAlimento>()
            .HasIndex(categoria => categoria.NomeNormalizado)
            .IsUnique();

        modelBuilder.Entity<Alimento>()
            .HasIndex(alimento => alimento.NomeNormalizado)
            .IsUnique();

        modelBuilder.Entity<Alimento>()
            .HasOne(alimento => alimento.Categoria)
            .WithMany()
            .HasForeignKey(alimento => alimento.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composição por 100 g como tipo complexo (L10): não tem identidade nem tabela
        // filha. O SQLite não aplica precisão nem escala; a garantia de duas casas vem da
        // validação do domínio (Spec 011, RNF1).
        modelBuilder.Entity<Alimento>()
            .ComplexProperty(alimento => alimento.Valores, valores =>
            {
                valores.Property(v => v.CaloriasKcal)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                valores.Property(v => v.ProteinasG)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                valores.Property(v => v.CarboidratosG)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                valores.Property(v => v.GordurasG)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                valores.Property(v => v.FibrasG)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                valores.Property(v => v.SodioMg)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();
            });

        // O vínculo é o par (alimento, restrição): a chave composta impede duplicata e não
        // existe coluna de motivo (Spec 011, RF4).
        modelBuilder.Entity<AlimentoRestricao>()
            .HasKey(vinculo => new { vinculo.AlimentoId, vinculo.RestricaoId });

        modelBuilder.Entity<Alimento>()
            .HasMany(alimento => alimento.Vinculos)
            .WithOne()
            .HasForeignKey(vinculo => vinculo.AlimentoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vinculos é IReadOnlyList sobre o campo privado _vinculos: sem HasField o EF não
        // materializa a coleção, porque a propriedade não tem setter (Spec 011, RNF3).
        modelBuilder.Entity<Alimento>()
            .Navigation(alimento => alimento.Vinculos)
            .HasField("_vinculos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<AlimentoRestricao>()
            .HasOne(vinculo => vinculo.Restricao)
            .WithMany()
            .HasForeignKey(vinculo => vinculo.RestricaoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Cardápio (Spec 012) ----------

        // Período como tipo complexo (L10): colunas Periodo_Inicio e Periodo_Fim, sem
        // tabela filha. A comparação de datas funciona lexicograficamente porque o SQLite
        // grava DateOnly como TEXT no formato ISO (yyyy-MM-dd).
        modelBuilder.Entity<Cardapio>()
            .ComplexProperty(cardapio => cardapio.Periodo, periodo =>
            {
                periodo.Property(p => p.Inicio).IsRequired();
                periodo.Property(p => p.Fim).IsRequired();
            });

        modelBuilder.Entity<Cardapio>()
            .HasOne<UnidadeEscolar>()
            .WithMany()
            .HasForeignKey(cardapio => cardapio.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cardapio>()
            .HasIndex(cardapio => new { cardapio.UnidadeId, cardapio.Situacao });

        modelBuilder.Entity<Cardapio>()
            .HasIndex(cardapio => cardapio.Situacao);

        // Autoria do cardápio (Spec 027): snapshot sem FK. Os limites espelham os do
        // cadastro de usuário; cardápios anteriores à regra ficam com texto vazio.
        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.NutricionistaNome)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.NutricionistaEmail)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.NutricionistaCpf)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.NutricionistaCrn)
            .HasMaxLength(20);

        // ---------- Cardápio geral e adaptado (Spec 029) ----------

        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.Tipo)
            .HasDefaultValue(TipoCardapio.Geral)
            .IsRequired();

        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.ConjuntoRestricoesChave)
            .HasMaxLength(ConjuntoDeRestricoes.TamanhoMaximoChave);

        // Origem sempre aponta para um geral; a exclusão do geral é barrada enquanto houver
        // versões adaptadas (Restrict).
        modelBuilder.Entity<Cardapio>()
            .HasOne<Cardapio>()
            .WithMany()
            .HasForeignKey(cardapio => cardapio.OrigemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cardapio>()
            .HasOne<Escola>()
            .WithMany()
            .HasForeignKey(cardapio => cardapio.EscolaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cardapio>()
            .HasIndex(cardapio => cardapio.OrigemId);

        // Uma versão adaptada vigente por (origem, escola, conjunto); versões substituídas
        // não bloqueiam a readaptação (Spec 029, RF3). O filtro usa os valores numéricos dos
        // enums porque é SQL gerado no model. Índices filtrados são suportados pelo SQLite.
        modelBuilder.Entity<Cardapio>()
            .HasIndex(cardapio => new
            {
                cardapio.OrigemId,
                cardapio.EscolaId,
                cardapio.ConjuntoRestricoesChave
            })
            .IsUnique()
            .HasFilter($"\"Tipo\" = {(int)TipoCardapio.Adaptado} AND \"Situacao\" <> {(int)SituacaoCardapio.Substituido}");

        // Ordem única por cardápio: duas refeições não podem ocupar o mesmo lugar (RF2).
        modelBuilder.Entity<Refeicao>()
            .HasIndex(refeicao => new { refeicao.CardapioId, refeicao.Ordem })
            .IsUnique();

        modelBuilder.Entity<Cardapio>()
            .HasMany(cardapio => cardapio.Refeicoes)
            .WithOne()
            .HasForeignKey(refeicao => refeicao.CardapioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cardapio>()
            .Navigation(cardapio => cardapio.Refeicoes)
            .HasField("_refeicoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Item não tem identidade própria: o par (refeição, alimento) é a chave e também
        // impede repetir o mesmo alimento na refeição (RF3).
        modelBuilder.Entity<ItemRefeicao>()
            .HasKey(item => new { item.RefeicaoId, item.AlimentoId });

        modelBuilder.Entity<Refeicao>()
            .HasMany(refeicao => refeicao.Itens)
            .WithOne()
            .HasForeignKey(item => item.RefeicaoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Refeicao>()
            .Navigation(refeicao => refeicao.Itens)
            .HasField("_itens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Alimento desativado continua referenciável por cardápio já existente (RF4).
        modelBuilder.Entity<ItemRefeicao>()
            .HasOne(item => item.Alimento)
            .WithMany()
            .HasForeignKey(item => item.AlimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Geração automática (Spec 013) ----------

        modelBuilder.Entity<ParametroGeracao>()
            .HasIndex(parametro => parametro.UnidadeId)
            .IsUnique();

        modelBuilder.Entity<ParametroGeracao>()
            .HasOne<UnidadeEscolar>()
            .WithMany()
            .HasForeignKey(parametro => parametro.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Coleções primitivas sobre campos privados, como Aluno.Restricoes (Spec 003).
        modelBuilder.Entity<ParametroGeracao>()
            .PrimitiveCollection(parametro => parametro.CategoriasObrigatorias)
            .HasField("_categoriasObrigatorias")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<ParametroGeracao>()
            .PrimitiveCollection(parametro => parametro.RefeicoesPadrao)
            .HasField("_refeicoesPadrao")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // ---------- Publicação e histórico (Spec 015) ----------

        // Concorrência otimista: Situacao e Versao entram no WHERE do UPDATE (ADR-010).
        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.Situacao)
            .IsConcurrencyToken();

        modelBuilder.Entity<Cardapio>()
            .Property(cardapio => cardapio.Versao)
            .IsConcurrencyToken();

        // Revisão não cascateia ao apagar o original (Restrict); original publicado não é apagado.
        modelBuilder.Entity<Cardapio>()
            .HasOne<Cardapio>()
            .WithMany()
            .HasForeignKey(cardapio => cardapio.RevisaoDeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Trilha append-only: FK com cascade (rascunho apagado leva seus eventos) e índice
        // por (CardapioId, CriadoEmUtc) para a linha do tempo (Spec 015, RNF5).
        modelBuilder.Entity<Cardapio>()
            .HasMany(cardapio => cardapio.Historico)
            .WithOne()
            .HasForeignKey(historico => historico.CardapioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cardapio>()
            .Navigation(cardapio => cardapio.Historico)
            .HasField("_historico")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<HistoricoCardapio>()
            .HasIndex(historico => new { historico.CardapioId, historico.CriadoEmUtc });

        // ---------- Substituições, pendências e confirmações (Spec 029) ----------

        // Registros filhos do agregado: sobem e descem com o cardápio (Cascade), e as
        // propriedades somente leitura são populadas pelo campo de apoio.
        modelBuilder.Entity<Cardapio>()
            .HasMany(cardapio => cardapio.Substituicoes)
            .WithOne()
            .HasForeignKey(substituicao => substituicao.CardapioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cardapio>()
            .Navigation(cardapio => cardapio.Substituicoes)
            .HasField("_substituicoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<SubstituicaoDeItem>()
            .Property(substituicao => substituicao.RefeicaoNome)
            .HasMaxLength(60)
            .IsRequired();

        modelBuilder.Entity<SubstituicaoDeItem>()
            .Property(substituicao => substituicao.AlimentoOriginalNome)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<SubstituicaoDeItem>()
            .Property(substituicao => substituicao.AlimentoSubstitutoNome)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<SubstituicaoDeItem>()
            .Property(substituicao => substituicao.RestricoesBloqueantes)
            .HasMaxLength(300)
            .IsRequired();

        modelBuilder.Entity<SubstituicaoDeItem>()
            .Property(substituicao => substituicao.QuantidadeGramas)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        modelBuilder.Entity<Cardapio>()
            .HasMany(cardapio => cardapio.Pendencias)
            .WithOne()
            .HasForeignKey(pendencia => pendencia.CardapioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cardapio>()
            .Navigation(cardapio => cardapio.Pendencias)
            .HasField("_pendencias")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<PendenciaAdaptacao>()
            .Property(pendencia => pendencia.RefeicaoNome)
            .HasMaxLength(60)
            .IsRequired();

        modelBuilder.Entity<PendenciaAdaptacao>()
            .Property(pendencia => pendencia.AlimentoNome)
            .HasMaxLength(100);

        modelBuilder.Entity<PendenciaAdaptacao>()
            .Property(pendencia => pendencia.Motivo)
            .HasMaxLength(500)
            .IsRequired();

        modelBuilder.Entity<Cardapio>()
            .HasMany(cardapio => cardapio.Confirmacoes)
            .WithOne()
            .HasForeignKey(confirmacao => confirmacao.CardapioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cardapio>()
            .Navigation(cardapio => cardapio.Confirmacoes)
            .HasField("_confirmacoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<ConfirmacaoDeIncompatibilidade>()
            .Property(confirmacao => confirmacao.AutorNome)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<ConfirmacaoDeIncompatibilidade>()
            .Property(confirmacao => confirmacao.AlimentoNome)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<ConfirmacaoDeIncompatibilidade>()
            .Property(confirmacao => confirmacao.Escolas)
            .HasMaxLength(500)
            .IsRequired();

        modelBuilder.Entity<ConfirmacaoDeIncompatibilidade>()
            .Property(confirmacao => confirmacao.Conjuntos)
            .HasMaxLength(500)
            .IsRequired();

        modelBuilder.Entity<ConfirmacaoDeIncompatibilidade>()
            .Property(confirmacao => confirmacao.CriadoEmUtc)
            .HasConversion(
                valor => valor,
                valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

        // ---------- Solicitações (Spec 017) ----------

        modelBuilder.Entity<Solicitacao>(entity =>
        {
            entity.Property(solicitacao => solicitacao.Descricao)
                .HasMaxLength(Solicitacao.DescricaoMax)
                .IsRequired();

            entity.Property(solicitacao => solicitacao.SolicitanteNome)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(solicitacao => solicitacao.RestricaoNome)
                .HasMaxLength(60);

            // Snapshots dos nomes do alvo (RNF3/ADR-009), preservados após renomear/excluir.
            entity.Property(solicitacao => solicitacao.AlunoNome).HasMaxLength(100);
            entity.Property(solicitacao => solicitacao.CardapioNome).HasMaxLength(120);

            entity.Property(solicitacao => solicitacao.Ingrediente)
                .HasMaxLength(120);

            entity.Property(solicitacao => solicitacao.Resposta)
                .HasMaxLength(Solicitacao.RespostaMax);

            entity.Property(solicitacao => solicitacao.RespondidoPor)
                .HasMaxLength(100);

            entity.Property(solicitacao => solicitacao.Status)
                .IsConcurrencyToken();

            entity.HasIndex(solicitacao => new { solicitacao.UnidadeId, solicitacao.CriadoEmUtc });
            entity.HasIndex(solicitacao => new { solicitacao.EscolaId, solicitacao.CriadoEmUtc });
            entity.HasIndex(solicitacao => new { solicitacao.Status, solicitacao.Tipo });

            entity.HasOne<UnidadeEscolar>()
                .WithMany()
                .HasForeignKey(solicitacao => solicitacao.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne<Escola>()
                .WithMany()
                .HasForeignKey(solicitacao => solicitacao.EscolaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Aluno>()
                .WithMany()
                .HasForeignKey(solicitacao => solicitacao.AlunoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Cardapio>()
                .WithMany()
                .HasForeignKey(solicitacao => solicitacao.CardapioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(solicitacao => solicitacao.Historico)
                .WithOne()
                .HasForeignKey(historico => historico.SolicitacaoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(solicitacao => solicitacao.Historico)
                .HasField("_historico")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(solicitacao => solicitacao.Alteracoes)
                .WithOne()
                .HasForeignKey(alteracao => alteracao.SolicitacaoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(solicitacao => solicitacao.Alteracoes)
                .HasField("_alteracoes")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<HistoricoSolicitacao>(entity =>
        {
            entity.Property(historico => historico.AutorNome)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(historico => historico.Resposta)
                .HasMaxLength(Solicitacao.RespostaMax);

            entity.HasIndex(historico => new { historico.SolicitacaoId, historico.CriadoEmUtc });
        });

        modelBuilder.Entity<SolicitacaoAlteracao>(entity =>
        {
            entity.Property(alteracao => alteracao.ValorSolicitado)
                .HasMaxLength(SolicitacaoAlteracao.ValorMaximo)
                .IsRequired();

            entity.Property(alteracao => alteracao.ValorNoPedido)
                .HasMaxLength(SolicitacaoAlteracao.ValorMaximo)
                .IsRequired();

            entity.HasIndex(alteracao => alteracao.SolicitacaoId);
        });

        modelBuilder.Entity<Notificacao>(entity =>
        {
            entity.Property(notificacao => notificacao.Titulo)
                .HasMaxLength(Notificacao.TituloMax)
                .IsRequired();

            entity.Property(notificacao => notificacao.Mensagem)
                .HasMaxLength(Notificacao.MensagemMax)
                .IsRequired();

            entity.Property(notificacao => notificacao.ReferenciaTipo)
                .HasMaxLength(Notificacao.ReferenciaTipoMax);

            entity.HasIndex(notificacao => new
            {
                notificacao.DestinatarioId,
                notificacao.Lida,
                notificacao.CriadoEmUtc
            });

            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(notificacao => notificacao.DestinatarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        // ---------- Ocorrências (Spec 016) ----------

        modelBuilder.Entity<Ocorrencia>(entity =>
        {
            entity.Property(ocorrencia => ocorrencia.Descricao)
                .HasMaxLength(Ocorrencia.DescricaoMax)
                .IsRequired();

            entity.Property(ocorrencia => ocorrencia.AutorNome)
                .HasMaxLength(100)
                .IsRequired();

            // Snapshots dos nomes do alvo (RNF3/ADR-009), preservados após renomear/excluir.
            entity.Property(ocorrencia => ocorrencia.AlunoNome).HasMaxLength(100);
            entity.Property(ocorrencia => ocorrencia.CardapioNome).HasMaxLength(120);
            entity.Property(ocorrencia => ocorrencia.RefeicaoNome).HasMaxLength(60);
            entity.Property(ocorrencia => ocorrencia.AlimentoNome).HasMaxLength(100);

            entity.Property(ocorrencia => ocorrencia.Status)
                .IsConcurrencyToken();

            entity.HasIndex(ocorrencia => new { ocorrencia.UnidadeId, ocorrencia.CriadoEmUtc });
            entity.HasIndex(ocorrencia => new { ocorrencia.EscolaId, ocorrencia.CriadoEmUtc });
            entity.HasIndex(ocorrencia => new { ocorrencia.AlunoId, ocorrencia.CriadoEmUtc });

            entity.HasOne<UnidadeEscolar>()
                .WithMany()
                .HasForeignKey(ocorrencia => ocorrencia.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne<Escola>()
                .WithMany()
                .HasForeignKey(ocorrencia => ocorrencia.EscolaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Aluno>()
                .WithMany()
                .HasForeignKey(ocorrencia => ocorrencia.AlunoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Cardapio>()
                .WithMany()
                .HasForeignKey(ocorrencia => ocorrencia.CardapioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Refeicao>()
                .WithMany()
                .HasForeignKey(ocorrencia => ocorrencia.RefeicaoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Alimento>()
                .WithMany()
                .HasForeignKey(ocorrencia => ocorrencia.AlimentoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(ocorrencia => ocorrencia.Historico)
                .WithOne()
                .HasForeignKey(historico => historico.OcorrenciaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(ocorrencia => ocorrencia.Historico)
                .HasField("_historico")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<HistoricoOcorrencia>(entity =>
        {
            entity.Property(historico => historico.AutorNome)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(historico => historico.Observacao)
                .HasMaxLength(Ocorrencia.ObservacaoMax);

            entity.HasIndex(historico => new { historico.OcorrenciaId, historico.CriadoEmUtc });
        });

        // ---------- Trilha de ação privilegiada (Spec 028) ----------
        modelBuilder.Entity<TrilhaAcao>(entity =>
        {
            entity.ToTable("TrilhaAcoes");
            entity.HasKey(acao => acao.Id);
            entity.Property(acao => acao.Tipo).HasMaxLength(60).IsRequired();
            entity.Property(acao => acao.AutorNome).HasMaxLength(100).IsRequired();
            entity.Property(acao => acao.AlvoTipo).HasMaxLength(60).IsRequired();
            entity.Property(acao => acao.ValorAnterior).HasMaxLength(300);
            entity.Property(acao => acao.ValorNovo).HasMaxLength(300);
            entity.Property(acao => acao.EmUtc)
                .HasConversion(
                    valor => valor,
                    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
            entity.HasIndex(acao => new { acao.AlvoTipo, acao.AlvoId });
            entity.HasIndex(acao => acao.EmUtc);
        });
    }
}