using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>Item do cardápio a analisar (Spec 014): refeição resolvida e alimento.</summary>
public sealed record ItemAnalisado(
    int RefeicaoOrdem,
    string RefeicaoNome,
    int AlimentoId,
    string AlimentoNome);

/// <summary>Vínculo alimento↔restrição com a severidade derivada da categoria (ADR-011).</summary>
public sealed record VinculoAnalisado(
    int RestricaoId,
    string RestricaoNome,
    CategoriaRestricao Categoria);

/// <summary>Aluno da unidade com as restrições (nomes) que carrega (Spec 010).</summary>
public sealed record AlunoAnalisado(
    int Id,
    string Nome,
    IReadOnlyList<string> Restricoes);

/// <summary>
/// Cruzamento puro entre itens do cardápio, vínculos alimento↔restrição e restrições dos
/// alunos da unidade (Spec 014, ADR-011). Não consulta banco, relógio nem serviço: recebe
/// os registros já carregados e devolve um resultado determinístico — mesma entrada, mesma
/// saída, com ordenação por desempate explícito (RNF3).
/// </summary>
public static class AnaliseCompatibilidade
{
    public static ResultadoCompatibilidade Analisar(
        IReadOnlyList<ItemAnalisado> itens,
        IReadOnlyDictionary<int, IReadOnlyList<VinculoAnalisado>> vinculosPorAlimento,
        IReadOnlyList<AlunoAnalisado> alunos)
    {
        // Agrupa os alunos pela restrição normalizada: a comparação usa a regra única de
        // `RestricaoAlimentar.Normalizar` (L9), nunca uma segunda normalização.
        var porRestricao = alunos
            .SelectMany(aluno => aluno.Restricoes.Select(restricao =>
                (Restricao: RestricaoAlimentar.Normalizar(restricao), Aluno: aluno)))
            .GroupBy(x => x.Restricao)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Select(x => (x.Aluno.Id, x.Aluno.Nome)).ToList());

        var achados = new List<AchadoCompatibilidade>();

        foreach (var item in itens
                     .OrderBy(item => item.RefeicaoOrdem)
                     .ThenBy(item => item.AlimentoNome, StringComparer.Ordinal))
        {
            if (!vinculosPorAlimento.TryGetValue(item.AlimentoId, out var vinculos))
                continue;

            foreach (var vinculo in vinculos.OrderBy(vinculo => vinculo.RestricaoNome, StringComparer.Ordinal))
            {
                if (!porRestricao.TryGetValue(
                        RestricaoAlimentar.Normalizar(vinculo.RestricaoNome),
                        out var afetados))
                {
                    // Alimento com vínculo cuja restrição não pertence a nenhum aluno: permitido.
                    continue;
                }

                achados.Add(new AchadoCompatibilidade(
                    item.RefeicaoOrdem,
                    item.RefeicaoNome,
                    item.AlimentoId,
                    item.AlimentoNome,
                    vinculo.RestricaoNome,
                    SeveridadeDeRestricao.De(vinculo.Categoria),
                    afetados.OrderBy(aluno => aluno.Nome, StringComparer.Ordinal).ToList()));
            }
        }

        var naoPermitidos = achados
            .Select(achado => achado.AlimentoNome)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(nome => nome, StringComparer.Ordinal)
            .ToList();

        var permitidos = itens
            .Select(item => item.AlimentoNome)
            .Distinct(StringComparer.Ordinal)
            .Except(naoPermitidos, StringComparer.Ordinal)
            .OrderBy(nome => nome, StringComparer.Ordinal)
            .ToList();

        return new ResultadoCompatibilidade(
            Compativel: achados.All(achado => achado.Severidade != SeveridadeCompatibilidade.Bloqueante),
            Bloqueantes: achados.Where(achado => achado.Severidade == SeveridadeCompatibilidade.Bloqueante).ToList(),
            Alertas: achados.Where(achado => achado.Severidade == SeveridadeCompatibilidade.Alerta).ToList(),
            AlimentosPermitidos: permitidos,
            AlimentosNaoPermitidos: naoPermitidos,
            TotalAlunosAnalisados: alunos.Count,
            Avisos: alunos.Count == 0
                ? new[] { "Nenhum aluno na unidade; a compatibilidade não foi analisada." }
                : Array.Empty<string>());
    }
}
