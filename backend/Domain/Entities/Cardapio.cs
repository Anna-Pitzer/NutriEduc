namespace Ntc.Domain.Entity;

/// <summary>
/// Agregado do cardápio (Spec 012, RF1 a RF13).
///
/// Reúne nome, unidade, período de vigência, situação, refeições e itens. Toda validação
/// passa por <c>Validar</c>, que devolve um buffer; só depois <c>Aplicar</c> toca no
/// estado. É o que garante RF12: um payload inválido não deixa o cardápio pela metade.
///
/// As transições de situação não vivem aqui nesta spec — quem as cria é a Spec 015,
/// junto das pré-condições. Método `internal` vazio seria código morto e convite a
/// chamada indevida.
/// </summary>
public class Cardapio
{
    public const int NomeMin = 2;
    public const int NomeMax = 120;
    public const int RefeicoesMaximas = 10;

    private readonly List<Refeicao> _refeicoes = new();

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public int UnidadeId { get; private set; }

    public PeriodoVigencia Periodo { get; private set; } = null!;

    public SituacaoCardapio Situacao { get; private set; }

    /// <summary>Origem da duplicação (RF6). A revisão da Spec 015 usa outro campo.</summary>
    public int? CardapioDuplicadoDeId { get; private set; }

    /// <summary>Versão do cardápio; quem incrementa é a revisão da Spec 015.</summary>
    public int Versao { get; private set; } = 1;

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    public IReadOnlyList<Refeicao> Refeicoes => _refeicoes.AsReadOnly();

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private Cardapio()
    {
    }

    public Cardapio(
        string? nome,
        int unidadeId,
        PeriodoVigencia? periodo,
        IEnumerable<Refeicao>? refeicoes,
        int? duplicadoDeId = null)
    {
        var dados = Validar(nome, unidadeId, periodo, refeicoes);

        Aplicar(dados);

        Situacao = SituacaoCardapio.Rascunho;
        CardapioDuplicadoDeId = duplicadoDeId;
        CriadoEmUtc = AtualizadoEmUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Substitui nome, período e refeições de um rascunho (RF12). Valida tudo antes de
    /// alterar qualquer campo; cardápio que não é rascunho não é editável.
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

    /// <summary>
    /// Cópia para duplicação (RF6) e para a revisão da Spec 015. Cria instâncias novas de
    /// refeição e item; o marcador de origem é parâmetro, não `origem.Id` fixo, para a
    /// revisão não nascer marcada como duplicata.
    /// </summary>
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
