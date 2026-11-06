using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Traduz os nomes de restrição guardados no aluno para o catálogo da Spec 010, que é a
/// fonte de identidade e de categoria (ADR-011).
///
/// Existe para que o contexto da montagem, a adaptação e a versão servida usem exatamente a
/// mesma tradução: nomes são comparados pela regra única de
/// <see cref="RestricaoAlimentar.Normalizar"/>, e o conjunto é sempre ordenado por id.
/// </summary>
public static class RestricoesDoAluno
{
    public static IReadOnlyDictionary<string, RestricaoAlimentar> PorNomeNormalizado(
        IEnumerable<RestricaoAlimentar> catalogo) =>
        catalogo
            .GroupBy(restricao => restricao.NomeNormalizado)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.OrderBy(restricao => restricao.Id).First());

    public static IReadOnlyList<RestricaoAlimentar> DoAluno(
        Aluno aluno,
        IReadOnlyDictionary<string, RestricaoAlimentar> porNome) =>
        (aluno.Restricoes ?? Array.Empty<string>())
            .Select(nome => porNome.GetValueOrDefault(RestricaoAlimentar.Normalizar(nome)))
            .Where(restricao => restricao is not null)
            .Select(restricao => restricao!)
            .OrderBy(restricao => restricao.Id)
            .ToList();

    /// <summary>
    /// Verdadeiro quando o aluno carrega um nome que não existe no catálogo. Nesse caso a
    /// versão servida não pode ser calculada com segurança e o resultado é
    /// <c>Indisponivel</c> (fail-closed).
    /// </summary>
    public static bool TemRestricaoSemCatalogo(
        Aluno aluno,
        IReadOnlyDictionary<string, RestricaoAlimentar> porNome) =>
        (aluno.Restricoes ?? Array.Empty<string>())
            .Any(nome => !porNome.ContainsKey(RestricaoAlimentar.Normalizar(nome)));

    public static ConjuntoDeRestricoes Conjunto(
        Aluno aluno,
        IReadOnlyDictionary<string, RestricaoAlimentar> porNome) =>
        ConjuntoDeRestricoes.DeIds(DoAluno(aluno, porNome).Select(restricao => restricao.Id));
}
