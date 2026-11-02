namespace Ntc.Domain.Entity;

/// <summary>
/// Refeição de um cardápio (Spec 012, RF2).
///
/// O nome é livre porque quem define a estrutura de refeições é a unidade, não o código
/// (Spec 013, `RefeicaoPadrao`). A ordem é única dentro do cardápio e os nomes precisam
/// ser distintos sem diferenciar caixa: o filtro público casa por nome (RF10), e duas
/// refeições homônimas tornariam o filtro ambíguo.
/// </summary>
public class Refeicao
{
    public const int NomeMin = 2;
    public const int NomeMax = 60;
    public const int ItensMaximos = 50;

    private readonly List<ItemRefeicao> _itens = new();

    public int Id { get; private set; }

    public int CardapioId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public int Ordem { get; private set; }

    public IReadOnlyList<ItemRefeicao> Itens => _itens.AsReadOnly();

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private Refeicao()
    {
    }

    internal Refeicao(string? nome, int ordem, IEnumerable<ItemRefeicao>? itens)
    {
        var dados = Validar(nome, ordem, itens);

        Nome = dados.Nome;
        Ordem = dados.Ordem;
        _itens.AddRange(dados.Itens);
    }

    /// <summary>
    /// Copia a refeição criando itens novos: compartilhar `ItemRefeicao` entre agregados
    /// violaria os limites do agregado (ADR-004).
    /// </summary>
    internal static Refeicao Copiar(Refeicao origem) =>
        new(
            origem.Nome,
            origem.Ordem,
            origem._itens.Select(item => new ItemRefeicao(item.AlimentoId, item.QuantidadeGramas)));

    private static (string Nome, int Ordem, List<ItemRefeicao> Itens) Validar(
        string? nome,
        int ordem,
        IEnumerable<ItemRefeicao>? itens)
    {
        var nomeValidado = ValidacaoDeCampo.Texto(nome, "nome da refeição", NomeMin, NomeMax);

        if (ordem <= 0)
        {
            throw new ArgumentException("A ordem da refeição deve ser positiva.");
        }

        var validados = (itens ?? Array.Empty<ItemRefeicao>()).ToList();

        if (validados.Count > ItensMaximos)
        {
            throw new ArgumentException($"A refeição aceita no máximo {ItensMaximos} itens.");
        }

        if (validados.GroupBy(item => item.AlimentoId).Any(grupo => grupo.Count() > 1))
        {
            throw new ArgumentException("O mesmo alimento não pode aparecer duas vezes na refeição.");
        }

        return (nomeValidado, ordem, validados);
    }
}
