namespace Ntc.Domain.Entity;

/// <summary>
/// Parâmetros de geração automática de cardápios por unidade (Spec 013, RF1 e RF2).
///
/// É único por unidade e imutável quanto à unidade depois de criado. Toda validação
/// acontece no construtor e em <see cref="Atualizar"/> — que compartilham <c>Validar</c> — e
/// só então os campos são atribuídos (ADR-004). Alterar os parâmetros não afeta cardápios
/// já gerados: o resultado fica no cardápio, não aqui.
/// </summary>
public class ParametroGeracao
{
    private readonly List<int> _categoriasObrigatorias = new();
    private readonly List<string> _refeicoesPadrao = new();

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private ParametroGeracao()
    {
    }

    public int Id { get; private set; }

    public int UnidadeId { get; private set; }

    public decimal CaloriasMinimasRefeicao { get; private set; }

    public decimal CaloriasMaximasRefeicao { get; private set; }

    public int OpcoesPorRefeicao { get; private set; }

    public int JanelaRepeticaoDias { get; private set; }

    public bool ExcluirAlimentosIncompativeis { get; private set; }

    public IReadOnlyList<int> CategoriasObrigatorias => _categoriasObrigatorias.AsReadOnly();

    /// <summary>Nomes das refeições padrão, na ordem em que aparecem no cardápio gerado.</summary>
    public IReadOnlyList<string> RefeicoesPadrao => _refeicoesPadrao.AsReadOnly();

    public ParametroGeracao(
        int unidadeId,
        decimal caloriasMinimas,
        decimal caloriasMaximas,
        int opcoesPorRefeicao,
        int janelaRepeticaoDias,
        bool excluirAlimentosIncompativeis,
        IEnumerable<int>? categoriasObrigatorias,
        IEnumerable<string>? refeicoesPadrao)
    {
        var dados = Validar(
            unidadeId,
            caloriasMinimas,
            caloriasMaximas,
            opcoesPorRefeicao,
            janelaRepeticaoDias,
            categoriasObrigatorias,
            refeicoesPadrao);

        Aplicar(dados, excluirAlimentosIncompativeis);
    }

    /// <summary>
    /// Redefine a faixa calórica, as opções, a janela, as categorias e as refeições padrão.
    /// A unidade não muda: o parâmetro pertence a uma unidade desde a criação.
    /// </summary>
    public void Atualizar(
        decimal caloriasMinimas,
        decimal caloriasMaximas,
        int opcoesPorRefeicao,
        int janelaRepeticaoDias,
        bool excluirAlimentosIncompativeis,
        IEnumerable<int>? categoriasObrigatorias,
        IEnumerable<string>? refeicoesPadrao)
    {
        var dados = Validar(
            UnidadeId,
            caloriasMinimas,
            caloriasMaximas,
            opcoesPorRefeicao,
            janelaRepeticaoDias,
            categoriasObrigatorias,
            refeicoesPadrao);

        Aplicar(dados, excluirAlimentosIncompativeis);
    }

    private void Aplicar(
        (int UnidadeId, decimal Min, decimal Max, int Opcoes, int Janela, List<int> Categorias, List<string> Refeicoes) dados,
        bool excluirAlimentosIncompativeis)
    {
        UnidadeId = dados.UnidadeId;
        CaloriasMinimasRefeicao = dados.Min;
        CaloriasMaximasRefeicao = dados.Max;
        OpcoesPorRefeicao = dados.Opcoes;
        JanelaRepeticaoDias = dados.Janela;
        ExcluirAlimentosIncompativeis = excluirAlimentosIncompativeis;

        _categoriasObrigatorias.Clear();
        _categoriasObrigatorias.AddRange(dados.Categorias);

        _refeicoesPadrao.Clear();
        _refeicoesPadrao.AddRange(dados.Refeicoes);
    }

    private static (
        int UnidadeId,
        decimal Min,
        decimal Max,
        int Opcoes,
        int Janela,
        List<int> Categorias,
        List<string> Refeicoes) Validar(
        int unidadeId,
        decimal caloriasMinimas,
        decimal caloriasMaximas,
        int opcoesPorRefeicao,
        int janelaRepeticaoDias,
        IEnumerable<int>? categoriasObrigatorias,
        IEnumerable<string>? refeicoesPadrao)
    {
        if (unidadeId <= 0)
            throw new ArgumentException("Parâmetros de geração exigem uma unidade válida.");

        if (caloriasMinimas < 0 || caloriasMaximas < 0)
            throw new ArgumentException("A faixa calórica não pode ser negativa.");

        if (caloriasMinimas > caloriasMaximas)
            throw new ArgumentException("O mínimo calórico não pode exceder o máximo.");

        if (opcoesPorRefeicao is < 1 or > 10)
            throw new ArgumentException("Opções por refeição deve estar entre 1 e 10.");

        if (janelaRepeticaoDias is < 0 or > 30)
            throw new ArgumentException("Janela de repetição deve estar entre 0 e 30 dias.");

        var categorias = new List<int>();

        foreach (var categoriaId in categoriasObrigatorias ?? Enumerable.Empty<int>())
        {
            if (categoriaId <= 0)
                throw new ArgumentException("Categoria obrigatória inválida.");

            if (categorias.Contains(categoriaId))
                throw new ArgumentException($"A categoria {categoriaId} está repetida.");

            categorias.Add(categoriaId);
        }

        var refeicoes = new List<string>();

        foreach (var nome in refeicoesPadrao ?? Enumerable.Empty<string>())
        {
            var valor = (nome ?? string.Empty).Trim();

            if (valor.Length == 0)
                throw new ArgumentException("O nome da refeição padrão não pode ser vazio.");

            if (refeicoes.Contains(valor, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"A refeição '{valor}' está repetida.");

            refeicoes.Add(valor);
        }

        if (refeicoes.Count == 0)
            throw new ArgumentException("Informe ao menos uma refeição padrão.");

        return (unidadeId, caloriasMinimas, caloriasMaximas, opcoesPorRefeicao, janelaRepeticaoDias, categorias, refeicoes);
    }
}
