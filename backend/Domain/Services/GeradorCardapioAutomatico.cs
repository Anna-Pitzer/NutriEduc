using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>
/// Gerador automático de cardápio (Spec 013, RF4 a RF9). Função pura e determinística:
/// <c>Random(seed)</c> é a única fonte de aleatoriedade; nenhuma ordenação depende de cultura
/// ou de hash; nenhuma leitura de relógio ou de banco acontece aqui (RNF1 e RNF2).
///
/// Cada item gerado representa uma porção de 100 g, então o total calórico de uma refeição
/// é a soma de <see cref="ValoresNutricionais.CaloriasKcal"/> dos itens selecionados. O
/// algoritmo é guloso (uma passagem por refeição), sem busca exaustiva (RNF3).
/// </summary>
public static class GeradorCardapioAutomatico
{
    public static ResultadoGeracao Gerar(EntradaGeracao entrada)
    {
        var random = new Random(entrada.Seed);
        var usados = new HashSet<int>();
        var refeicoes = new List<(RefeicaoPadrao Refeicao, IReadOnlyList<ItemGerado> Itens)>();
        var avisos = new List<string>();

        foreach (var refeicao in entrada.Refeicoes
                     .OrderBy(refeicao => refeicao.Ordem)
                     .ThenBy(refeicao => refeicao.Nome, StringComparer.Ordinal))
        {
            var itens = GerarRefeicao(
                refeicao,
                entrada.Candidatos,
                entrada.AlimentosBloqueados,
                entrada.Parametros,
                usados,
                random,
                avisos);

            refeicoes.Add((refeicao, itens));
        }

        return new ResultadoGeracao(refeicoes, entrada.Seed, avisos);
    }

    private static IReadOnlyList<ItemGerado> GerarRefeicao(
        RefeicaoPadrao refeicao,
        IReadOnlyList<CandidatoAlimento> candidatos,
        IReadOnlyCollection<int> bloqueados,
        ParametroGeracao parametros,
        HashSet<int> usados,
        Random random,
        ICollection<string> avisos)
    {
        var disponiveis = candidatos
            .Where(candidato => !bloqueados.Contains(candidato.Id) && !usados.Contains(candidato.Id))
            .ToList();

        Embaralhar(disponiveis, random);

        var selecionados = new List<ItemGerado>();
        var idsSelecionados = new HashSet<int>();

        // Categorias obrigatórias primeiro (RF6): uma por categoria, em ordem estável.
        foreach (var categoriaId in parametros.CategoriasObrigatorias)
        {
            var daCategoria = disponiveis.Where(candidato => candidato.CategoriaId == categoriaId).ToList();

            if (daCategoria.Count == 0)
                throw new GeracaoImpossivelException(
                    refeicao.Nome,
                    $"Nenhum alimento disponível para a categoria obrigatória {categoriaId}.");

            if (daCategoria.Count == 1)
                avisos.Add($"Categoria obrigatória {categoriaId} atendida por um único alimento.");

            var candidato = daCategoria.First(item => !idsSelecionados.Contains(item.Id));
            selecionados.Add(ParaItem(candidato));
            idsSelecionados.Add(candidato.Id);
        }

        // Completa até OpcoesPorRefeicao alimentos distintos (RF4).
        foreach (var candidato in disponiveis)
        {
            if (selecionados.Count >= parametros.OpcoesPorRefeicao)
                break;

            if (idsSelecionados.Contains(candidato.Id))
                continue;

            selecionados.Add(ParaItem(candidato));
            idsSelecionados.Add(candidato.Id);
        }

        if (selecionados.Count < parametros.OpcoesPorRefeicao)
            throw new GeracaoImpossivelException(
                refeicao.Nome,
                "Alimentos insuficientes para a quantidade de opções por refeição.");

        var totalCalorias = selecionados.Sum(item => item.Valores.CaloriasKcal);

        if (totalCalorias < parametros.CaloriasMinimasRefeicao ||
            totalCalorias > parametros.CaloriasMaximasRefeicao)
        {
            throw new GeracaoImpossivelException(
                refeicao.Nome,
                $"Faixa calórica {parametros.CaloriasMinimasRefeicao:0.#}–{parametros.CaloriasMaximasRefeicao:0.#} kcal inatingível.");
        }

        foreach (var id in idsSelecionados)
            usados.Add(id);

        return selecionados;
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
