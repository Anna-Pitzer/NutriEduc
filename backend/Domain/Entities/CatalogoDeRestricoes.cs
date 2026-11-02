namespace Ntc.Domain.Entity;

/// <summary>
/// Catálogo de restrições já carregado, usado pelo domínio para resolver os nomes que o
/// aluno informa (Spec 010, RF4).
///
/// Existe para que a regra de vínculo não viva no serviço: o caso de uso apenas carrega as
/// entradas ativas pelo repositório e entrega este objeto, que decide o que é válido e
/// devolve sempre o nome canônico do catálogo. Aluno, catálogo e histórico comparam nomes
/// pela mesma normalização — <see cref="RestricaoAlimentar.Normalizar"/>.
/// </summary>
public sealed class CatalogoDeRestricoes
{
    private readonly Dictionary<string, RestricaoAlimentar> _porNomeNormalizado;

    public CatalogoDeRestricoes(IEnumerable<RestricaoAlimentar>? entradas)
    {
        _porNomeNormalizado = new Dictionary<string, RestricaoAlimentar>(StringComparer.Ordinal);

        foreach (var entrada in entradas ?? Enumerable.Empty<RestricaoAlimentar>())
        {
            // Item inativo continua existindo para leitura do aluno, mas não é vinculável
            // (RF3): por isso só as ativas entram na resolução de novos vínculos.
            if (entrada.Ativa)
            {
                _porNomeNormalizado.TryAdd(entrada.NomeNormalizado, entrada);
            }
        }
    }

    /// <summary>
    /// Nomes informados, sem espaços nas pontas, sem vazios e sem repetição que ignore caixa.
    /// É o insumo da consulta ao repositório e da validação.
    /// </summary>
    public static IReadOnlyList<string> NomesInformados(IEnumerable<string>? nomes)
    {
        return (nomes ?? Enumerable.Empty<string>())
            .Select(nome => (nome ?? string.Empty).Trim())
            .Where(nome => nome.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Nomes normalizados para a consulta única ao catálogo.</summary>
    public static IReadOnlyList<string> NomesNormalizados(IEnumerable<string> nomes)
    {
        return nomes
            .Select(RestricaoAlimentar.Normalizar)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Traduz os nomes informados para os nomes canônicos do catálogo. Nome inexistente ou
    /// inativo lança <see cref="ArgumentException"/> nomeando o valor informado, para o
    /// usuário corrigir o catálogo — não para o vínculo ser aceito parcialmente.
    /// </summary>
    public IReadOnlyList<string> ResolverNomes(IEnumerable<string> nomes)
    {
        var canonicos = new List<string>();

        foreach (var informado in NomesInformados(nomes))
        {
            if (!_porNomeNormalizado.TryGetValue(RestricaoAlimentar.Normalizar(informado), out var entrada))
            {
                throw new ArgumentException($"Restrição desconhecida: {informado}.");
            }

            if (!canonicos.Contains(entrada.Nome, StringComparer.OrdinalIgnoreCase))
            {
                canonicos.Add(entrada.Nome);
            }
        }

        return canonicos;
    }
}
