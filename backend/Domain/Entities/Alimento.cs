namespace Ntc.Domain.Entity;

/// <summary>
/// Alimento do catálogo (Spec 011, RF2 a RF6).
///
/// O agregado reúne nome, categoria, estado ativo, composição por 100 g e os vínculos
/// com restrições. Não existe campo "permitido": o que é permitido se deriva da ausência
/// de vínculo (RF5), para não haver duas fontes de verdade.
///
/// A desativação é soft delete (RF6): o registro continua legível e referenciável por
/// cardápios já publicados; bloquear cardápio novo é regra da Spec 012.
///
/// Toda validação acontece no construtor e em <see cref="AtualizarDados"/>, que usam o
/// mesmo <c>ValidarDados</c> e atribuem só depois de validar tudo (ADR-004): entrada
/// inválida não deixa estado parcial.
/// </summary>
public class Alimento
{
    private const int NomeMin = 2;
    private const int NomeMax = 100;

    private readonly List<AlimentoRestricao> _vinculos = new();

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private Alimento()
    {
    }

    public Alimento(
        string nome,
        int categoriaId,
        ValoresNutricionais valores,
        IEnumerable<int>? restricaoIds)
    {
        var dados = ValidarDados(nome, categoriaId, valores);
        var vinculos = NormalizarVinculos(restricaoIds);

        Nome = dados.Nome;
        NomeNormalizado = dados.NomeNormalizado;
        CategoriaId = categoriaId;
        Valores = valores;
        Ativo = true;

        foreach (var vinculo in vinculos)
            _vinculos.Add(vinculo);
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>Nome sem caixa e sem acento, persistido para o índice único global.</summary>
    public string NomeNormalizado { get; private set; } = string.Empty;

    public int CategoriaId { get; private set; }

    public bool Ativo { get; private set; }

    public ValoresNutricionais Valores { get; private set; } = null!;

    public IReadOnlyList<AlimentoRestricao> Vinculos => _vinculos.AsReadOnly();

    /// <summary>Categoria do alimento, carregada nas consultas de leitura.</summary>
    public CategoriaAlimento? Categoria { get; private set; }

    /// <summary>
    /// Redefine nome, categoria, composição e vínculos. Valida tudo num buffer antes de
    /// tocar no estado: uma entrada inválida não altera nenhum campo.
    /// </summary>
    public void AtualizarDados(
        string nome,
        int categoriaId,
        ValoresNutricionais valores,
        IEnumerable<int>? restricaoIds)
    {
        var dados = ValidarDados(nome, categoriaId, valores);
        var vinculos = NormalizarVinculos(restricaoIds);

        Nome = dados.Nome;
        NomeNormalizado = dados.NomeNormalizado;
        CategoriaId = categoriaId;
        Valores = valores;

        _vinculos.Clear();

        foreach (var vinculo in vinculos)
            _vinculos.Add(vinculo);
    }

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    /// <summary>Atalho de leitura usado pela Spec 014 para saber se há incompatibilidade.</summary>
    public bool PossuiVinculo(int restricaoId) =>
        _vinculos.Any(vinculo => vinculo.RestricaoId == restricaoId);

    private static (string Nome, string NomeNormalizado) ValidarDados(
        string? nome,
        int categoriaId,
        ValoresNutricionais? valores)
    {
        var nomeValidado = ValidacaoDeCampo.Texto(nome, "Nome do alimento", NomeMin, NomeMax);

        if (categoriaId <= 0)
            throw new ArgumentException("Categoria do alimento inválida.");

        if (valores is null)
            throw new ArgumentException("Os valores nutricionais são obrigatórios.");

        return (nomeValidado, RestricaoAlimentar.Normalizar(nomeValidado));
    }

    /// <summary>
    /// Converte os ids em vínculos e recusa o mesmo par duas vezes (RF4): a chave
    /// composta já impediria no banco, mas a mensagem precisa nomear o problema antes de
    /// qualquer gravação.
    /// </summary>
    private static List<AlimentoRestricao> NormalizarVinculos(IEnumerable<int>? restricaoIds)
    {
        var vinculos = new List<AlimentoRestricao>();

        foreach (var restricaoId in restricaoIds ?? Enumerable.Empty<int>())
        {
            var vinculo = new AlimentoRestricao(restricaoId);

            if (vinculos.Any(existente => existente.RestricaoId == vinculo.RestricaoId))
                throw new ArgumentException(
                    $"A restrição {restricaoId} já está vinculada a este alimento.");

            vinculos.Add(vinculo);
        }

        return vinculos;
    }
}
