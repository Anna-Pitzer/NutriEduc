using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>Diagnóstico de uma refeição que não pôde ser gerada (Spec 032, RF15).</summary>
public sealed record DiagnosticoRefeicao(
    string Refeicao,
    int Tentativas,
    decimal FaixaMinimaAlcancavel,
    decimal FaixaMaximaAlcancavel,
    int? CategoriaAusente,
    int CandidatosDisponiveis);

/// <summary>Quantidade de tentativas usadas por uma refeição gerada (Spec 032, RF12).</summary>
public sealed record TentativaRefeicao(string Refeicao, int Tentativas);

/// <summary>
/// Função pura de seleção por refeição (Spec 032, RF5/RF6/RF15). Substitui a passagem
/// gulosa única da Spec 013 por até 5 tentativas determinísticas.
///
/// A semente da tentativa <c>n</c> deriva de <c>seed * 31 + n * 17 + ordem</c>; cada tentativa
/// embaralha uma cópia dos candidatos com <c>Random(semente)</c> e tenta fechar a refeição.
/// Nenhuma fonte global de aleatoriedade (RNF2). O pool já chega sem a janela de repetição e
/// sem os alimentos usados em refeições anteriores (RF4).
/// </summary>
public static class SeletorDeRefeicao
{
    public const int TentativasMaximas = 5;

    public static IReadOnlyList<ItemGerado>? TentarRefeicao(
        RefeicaoPadrao refeicao,
        IReadOnlyList<CandidatoAlimento> pool,
        ParametroGeracao parametros,
        int seed,
        out DiagnosticoRefeicao diagnostico)
    {
        // Categoria obrigatória sem nenhum alimento ativo no pool é falha estrutural (RF15).
        foreach (var categoriaId in parametros.CategoriasObrigatorias)
        {
            if (!pool.Any(candidato => candidato.CategoriaId == categoriaId))
            {
                diagnostico = new DiagnosticoRefeicao(
                    refeicao.Nome, 0, 0m, 0m, categoriaId, pool.Count);
                return null;
            }
        }

        decimal minimoAlcancado = decimal.MaxValue;
        decimal maximoAlcancado = decimal.MinValue;

        for (var tentativa = 1; tentativa <= TentativasMaximas; tentativa++)
        {
            var semente = unchecked(seed * 31 + tentativa * 17 + refeicao.Ordem);
            var random = new Random(semente);
            var disponiveis = pool.ToList();
            Embaralhar(disponiveis, random);

            var selecionados = new List<ItemGerado>();
            var ids = new HashSet<int>();

            // Categorias obrigatórias primeiro, na ordem declarada (RF6).
            foreach (var categoriaId in parametros.CategoriasObrigatorias)
            {
                var daCategoria = disponiveis.First(
                    candidato => candidato.CategoriaId == categoriaId && !ids.Contains(candidato.Id));

                selecionados.Add(ParaItem(daCategoria));
                ids.Add(daCategoria.Id);
            }

            // Completa até OpcoesPorRefeicao alimentos distintos.
            foreach (var candidato in disponiveis)
            {
                if (selecionados.Count >= parametros.OpcoesPorRefeicao)
                    break;

                if (ids.Contains(candidato.Id))
                    continue;

                selecionados.Add(ParaItem(candidato));
                ids.Add(candidato.Id);
            }

            var total = selecionados.Sum(item => item.Valores.CaloriasKcal);
            minimoAlcancado = Math.Min(minimoAlcancado, total);
            maximoAlcancado = Math.Max(maximoAlcancado, total);

            if (selecionados.Count == parametros.OpcoesPorRefeicao &&
                total >= parametros.CaloriasMinimasRefeicao &&
                total <= parametros.CaloriasMaximasRefeicao)
            {
                diagnostico = new DiagnosticoRefeicao(
                    refeicao.Nome, tentativa, minimoAlcancado, maximoAlcancado, null, pool.Count);
                return selecionados;
            }
        }

        diagnostico = new DiagnosticoRefeicao(
            refeicao.Nome,
            TentativasMaximas,
            minimoAlcancado == decimal.MaxValue ? 0m : minimoAlcancado,
            maximoAlcancado == decimal.MinValue ? 0m : maximoAlcancado,
            null,
            pool.Count);

        return null;
    }

    private static ItemGerado ParaItem(CandidatoAlimento candidato) =>
        new(candidato.Id, candidato.CategoriaId, candidato.Valores);

    private static void Embaralhar<T>(IList<T> lista, Random random)
    {
        for (var i = lista.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
