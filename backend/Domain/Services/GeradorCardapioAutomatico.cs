using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>
/// Gerador automático de cardápio (Spec 013, RF4 a RF9; Spec 032). Função pura e
/// determinística: a única fonte de aleatoriedade é <c>Random(semente)</c>, derivada de
/// <c>(seed, tentativa, ordem)</c> em <see cref="SeletorDeRefeicao"/>. Nenhuma ordenação
/// depende de cultura ou hash; nenhuma leitura de relógio ou de banco (RNF1/RNF2).
///
/// A Spec 032 substitui a passagem gulosa única por até 5 tentativas por refeição. Quando
/// uma refeição não fecha, o gerador lança <see cref="GeracaoImpossivelException"/> com o
/// diagnóstico por refeição (RF15); nenhum cardápio parcial é devolvido.
/// </summary>
public static class GeradorCardapioAutomatico
{
    public static ResultadoGeracao Gerar(EntradaGeracao entrada)
    {
        var usados = new HashSet<int>();
        var refeicoes = new List<(RefeicaoPadrao Refeicao, IReadOnlyList<ItemGerado> Itens)>();
        var avisos = new List<string>();
        var tentativas = new List<TentativaRefeicao>();

        foreach (var refeicao in entrada.Refeicoes
                     .OrderBy(refeicao => refeicao.Ordem)
                     .ThenBy(refeicao => refeicao.Nome, StringComparer.Ordinal))
        {
            var pool = entrada.Candidatos
                .Where(candidato =>
                    !entrada.AlimentosBloqueados.Contains(candidato.Id) &&
                    !usados.Contains(candidato.Id))
                .ToList();

            var itens = SeletorDeRefeicao.TentarRefeicao(
                refeicao, pool, entrada.Parametros, entrada.Seed, out var diagnostico);

            if (itens is null)
            {
                throw new GeracaoImpossivelException(new[] { diagnostico });
            }

            foreach (var item in itens)
                usados.Add(item.AlimentoId);

            refeicoes.Add((refeicao, itens));
            tentativas.Add(new TentativaRefeicao(refeicao.Nome, diagnostico.Tentativas));
        }

        return new ResultadoGeracao(refeicoes, entrada.Seed, avisos, tentativas);
    }
}
