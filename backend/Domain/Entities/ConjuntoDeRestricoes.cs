namespace Ntc.Domain.Entity;

/// <summary>
/// Conjunto de restrições alimentares que identifica uma versão adaptada (Spec 029, RF2).
///
/// Value object: normaliza os identificadores (positivos, distintos e em ordem crescente),
/// expõe a chave canônica separada por vírgula e compara por valor. A ordem de entrada e
/// eventuais duplicatas não alteram a chave, o que garante a idempotência da adaptação
/// (RNF5) e uma identidade estável no banco.
///
/// O conjunto sozinho não identifica uma versão: duas escolas com o mesmo conjunto têm
/// versões distintas. A identidade completa é <c>(origem, escola, chave)</c>, exposta por
/// <see cref="ChaveDeVersao"/>.
/// </summary>
public sealed class ConjuntoDeRestricoes : IEquatable<ConjuntoDeRestricoes>
{
    public const int TamanhoMaximoChave = 500;

    private readonly int[] _ids;

    private ConjuntoDeRestricoes(int[] ids)
    {
        _ids = ids;
    }

    /// <summary>Conjunto sem nenhuma restrição; não gera versão adaptada (RF2, RF21).</summary>
    public static ConjuntoDeRestricoes Vazio { get; } = new(Array.Empty<int>());

    public IReadOnlyList<int> Ids => _ids;

    public bool EstaVazio => _ids.Length == 0;

    /// <summary>Chave canônica, por exemplo <c>"3,7,12"</c>.</summary>
    public string Chave => string.Join(",", _ids);

    /// <summary>Normaliza a coleção recebida; identificadores não positivos são descartados.</summary>
    public static ConjuntoDeRestricoes DeIds(IEnumerable<int>? ids)
    {
        var normalizados = (ids ?? Array.Empty<int>())
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var chave = string.Join(",", normalizados);

        if (chave.Length > TamanhoMaximoChave)
        {
            throw new ArgumentException(
                $"O conjunto de restrições aceita no máximo {TamanhoMaximoChave} caracteres.");
        }

        return normalizados.Length == 0 ? Vazio : new ConjuntoDeRestricoes(normalizados);
    }

    /// <summary>
    /// Reconstrói o conjunto a partir da chave persistida. A chave é sempre normalizada;
    /// uma chave vazia representa o conjunto vazio.
    /// </summary>
    public static ConjuntoDeRestricoes DaChave(string? chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            return Vazio;
        }

        var partes = chave.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (partes.Length == 0)
        {
            return Vazio;
        }

        var ids = new List<int>(partes.Length);

        foreach (var parte in partes)
        {
            if (!int.TryParse(parte, out var id) || id <= 0)
            {
                throw new ArgumentException($"O conjunto de restrições contém um identificador inválido: '{parte}'.");
            }

            ids.Add(id);
        }

        return DeIds(ids);
    }

    public bool Contem(int restricaoId) => Array.BinarySearch(_ids, restricaoId) >= 0;

    /// <summary>Verdadeiro quando o conjunto contém todos os identificadores informados.</summary>
    public bool ContemTodos(IEnumerable<int> ids) => ids.All(Contem);

    /// <summary>Identidade composta usada para localizar a versão adaptada de uma escola.</summary>
    public string ChaveDeVersao(int origemId, int escolaId) => $"{origemId}:{escolaId}:{Chave}";

    public bool Equals(ConjuntoDeRestricoes? other) =>
        other is not null && Chave == other.Chave;

    public override bool Equals(object? obj) => obj is ConjuntoDeRestricoes outro && Equals(outro);

    public override int GetHashCode() => Chave.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Chave;
}
