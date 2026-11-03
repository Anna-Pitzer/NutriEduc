using Ntc.Domain.Services;

namespace Ntc.Domain.Entity;

/// <summary>
/// Agregado do cardápio (Spec 012, RF1 a RF13; Spec 015, RF1 a RF9).
///
/// Reúne nome, unidade, período de vigência, situação, versão, refeições, itens e a trilha
/// append-only de eventos. Toda validação passa por <c>Validar</c>, que devolve um buffer; só
/// depois <c>Aplicar</c> toca no estado (ADR-004).
///
/// As transições de situação vivem aqui (Spec 015, RNF1): pré-condições e gravação de evento
/// no próprio agregado. O serviço apenas carrega dados, chama o domínio e persiste.
/// </summary>
public class Cardapio
{
    public const int NomeMin = 2;
    public const int NomeMax = 120;
    public const int RefeicoesMaximas = 10;

    private readonly List<Refeicao> _refeicoes = new();
    private readonly List<HistoricoCardapio> _historico = new();

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public int UnidadeId { get; private set; }

    public PeriodoVigencia Periodo { get; private set; } = null!;

    public SituacaoCardapio Situacao { get; private set; }

    /// <summary>Origem da duplicação (Spec 012, RF6).</summary>
    public int? CardapioDuplicadoDeId { get; private set; }

    /// <summary>Origem da revisão (Spec 015, RF6); nulo quando o cardápio não é uma revisão.</summary>
    public int? RevisaoDeId { get; private set; }

    /// <summary>Versão do cardápio; incrementada pela revisão (Spec 015, RF6).</summary>
    public int Versao { get; private set; } = 1;

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    /// <summary>Preenchido na publicação (Spec 015, RF2); nulo em rascunho.</summary>
    public DateTime? PublicadoEmUtc { get; private set; }

    /// <summary>Snapshot do nome de quem publicou (Spec 015, RF2).</summary>
    public string PublicadoPor { get; private set; } = string.Empty;

    public IReadOnlyList<Refeicao> Refeicoes => _refeicoes.AsReadOnly();

    public IReadOnlyList<HistoricoCardapio> Historico => _historico.AsReadOnly();

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private Cardapio()
    {
    }

    public Cardapio(
        string? nome,
        int unidadeId,
        PeriodoVigencia? periodo,
        IEnumerable<Refeicao>? refeicoes,
        int? duplicadoDeId = null,
        int? revisaoDeId = null,
        int versao = 1)
    {
        var dados = Validar(nome, unidadeId, periodo, refeicoes);

        Aplicar(dados);

        Situacao = SituacaoCardapio.Rascunho;
        CardapioDuplicadoDeId = duplicadoDeId;
        RevisaoDeId = revisaoDeId;
        Versao = versao;
        CriadoEmUtc = AtualizadoEmUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Substitui nome, período e refeições de um rascunho (Spec 012, RF12). Valida tudo antes
    /// de alterar qualquer campo; cardápio que não é rascunho não é editável.
    /// </summary>
    public void Atualizar(
        string? nome,
        PeriodoVigencia? periodo,
        IEnumerable<Refeicao>? refeicoes)
    {
        if (Situacao != SituacaoCardapio.Rascunho)
        {
            throw new InvalidOperationException("Só é possível editar cardápio em rascunho.");
        }

        var dados = Validar(nome, UnidadeId, periodo, refeicoes);

        Aplicar(dados);
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    // ---------- Transições (Spec 015) ----------

    public void Publicar(string autorNome, DateTime utc)
    {
        if (Situacao == SituacaoCardapio.Publicado)
            throw new TransicaoInvalidaException("Cardápio já publicado.");
        if (Situacao != SituacaoCardapio.Rascunho)
            throw new TransicaoInvalidaException("Somente rascunho pode ser publicado.");

        Situacao = SituacaoCardapio.Publicado;
        PublicadoEmUtc = utc;
        PublicadoPor = string.IsNullOrWhiteSpace(autorNome) ? "sistema" : autorNome.Trim();
        AtualizadoEmUtc = utc;
        RegistrarEvento(EventoCardapio.Publicado, autorNome, null, utc);
    }

    public void Cancelar(string motivo, string autorNome, DateTime utc)
    {
        motivo = (motivo ?? string.Empty).Trim();

        if (motivo.Length < 2 || motivo.Length > 500)
            throw new ArgumentException("O motivo deve ter entre 2 e 500 caracteres.");

        if (Situacao is SituacaoCardapio.Cancelado or SituacaoCardapio.Substituido)
            throw new TransicaoInvalidaException("Cardápio já encerrado.");

        Situacao = SituacaoCardapio.Cancelado;
        AtualizadoEmUtc = utc;
        RegistrarEvento(EventoCardapio.Cancelado, autorNome, motivo, utc);
    }

    public Cardapio CriarRevisao(string autorNome, DateTime utc)
    {
        if (Situacao != SituacaoCardapio.Publicado)
            throw new TransicaoInvalidaException("Somente cardápio publicado pode ser revisado.");

        var revisao = new Cardapio(
            Nome,
            UnidadeId,
            Periodo,
            ClonarRefeicoes(),
            duplicadoDeId: null,
            revisaoDeId: Id,
            versao: Versao + 1);

        revisao.RegistrarEvento(EventoCardapio.RevisaoCriada, autorNome, $"Revisão de #{Id}", utc);

        return revisao;
    }

    internal void MarcarSubstituido(int porCardapioId, string autorNome, DateTime utc)
    {
        if (Situacao != SituacaoCardapio.Publicado)
            throw new TransicaoInvalidaException("Somente publicado pode ser substituído.");

        Situacao = SituacaoCardapio.Substituido;
        AtualizadoEmUtc = utc;
        RegistrarEvento(EventoCardapio.Substituido, autorNome, $"Substituído por #{porCardapioId}", utc);
    }

    // ---------- Registro de eventos ----------

    /// <summary>Evento de criação (Spec 015, RF9). Chamado pelo serviço logo após construir.</summary>
    public void RegistrarCriacao(string? autorNome) =>
        RegistrarEvento(EventoCardapio.Criado, autorNome ?? "sistema", null, DateTime.UtcNow);

    /// <summary>Evento de edição de rascunho (Spec 015, RF9).</summary>
    public void RegistrarEdicao(string? autorNome) =>
        RegistrarEvento(EventoCardapio.Editado, autorNome ?? "sistema", null, DateTime.UtcNow);

    /// <summary>Evento de duplicação (Spec 015, RF9).</summary>
    public void RegistrarDuplicacao(string? autorNome) =>
        RegistrarEvento(EventoCardapio.Duplicado, autorNome ?? "sistema", null, DateTime.UtcNow);

    private void RegistrarEvento(EventoCardapio evento, string autorNome, string? motivo, DateTime utc)
    {
        _historico.Add(new HistoricoCardapio(Id, evento, Versao, autorNome, motivo, utc));
    }

    /// <summary>Cópia para duplicação (Spec 012, RF6) e para a revisão (Spec 015, RF6).</summary>
    internal static Cardapio Copiar(
        Cardapio origem,
        string nome,
        PeriodoVigencia periodo,
        int? duplicadoDeId = null)
    {
        var refeicoes = origem._refeicoes.Select(Refeicao.Copiar);

        return new Cardapio(nome, origem.UnidadeId, periodo, refeicoes, duplicadoDeId);
    }

    /// <summary>Quantidade total de itens, usada na listagem sem carregar as refeições.</summary>
    public int TotalDeItens => _refeicoes.Sum(refeicao => refeicao.Itens.Count);

    private IEnumerable<Refeicao> ClonarRefeicoes() => _refeicoes.Select(Refeicao.Copiar);

    private void Aplicar((string Nome, int UnidadeId, PeriodoVigencia Periodo, List<Refeicao> Refeicoes) dados)
    {
        Nome = dados.Nome;
        UnidadeId = dados.UnidadeId;
        Periodo = dados.Periodo;

        _refeicoes.Clear();
        _refeicoes.AddRange(dados.Refeicoes);
    }

    private static (string Nome, int UnidadeId, PeriodoVigencia Periodo, List<Refeicao> Refeicoes) Validar(
        string? nome,
        int unidadeId,
        PeriodoVigencia? periodo,
        IEnumerable<Refeicao>? refeicoes)
    {
        var nomeValidado = ValidacaoDeCampo.Texto(nome, "nome", NomeMin, NomeMax);

        if (unidadeId <= 0)
        {
            throw new ArgumentException("O cardápio exige uma unidade válida.");
        }

        if (periodo is null)
        {
            throw new ArgumentException("O período do cardápio é obrigatório.");
        }

        var validados = (refeicoes ?? Array.Empty<Refeicao>()).ToList();

        if (validados.Count > RefeicoesMaximas)
        {
            throw new ArgumentException($"O cardápio aceita no máximo {RefeicoesMaximas} refeições.");
        }

        if (validados.GroupBy(refeicao => refeicao.Ordem).Any(grupo => grupo.Count() > 1))
        {
            throw new ArgumentException("Duas refeições não podem ter a mesma ordem.");
        }

        if (validados.GroupBy(refeicao => refeicao.Nome, StringComparer.OrdinalIgnoreCase)
            .Any(grupo => grupo.Count() > 1))
        {
            throw new ArgumentException("Duas refeições não podem ter o mesmo nome.");
        }

        return (nomeValidado, unidadeId, periodo, validados);
    }
}
