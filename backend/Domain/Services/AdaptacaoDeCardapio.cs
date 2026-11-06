using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>Restrição do catálogo com a categoria que define a severidade (Spec 010, ADR-011).</summary>
public sealed record RestricaoDoCatalogo(int Id, string Nome, CategoriaRestricao Categoria);

/// <summary>Escola e conjunto de restrições para os quais o geral será verificado (Spec 029).</summary>
public sealed record GrupoDeAdaptacao(int EscolaId, string EscolaNome, ConjuntoDeRestricoes Conjunto);

/// <summary>Substituição efetivamente aplicada a um item da versão adaptada (Spec 029, RF5).</summary>
public sealed record ItemSubstituido(
    int RefeicaoOrdem,
    string RefeicaoNome,
    int AlimentoOriginalId,
    string AlimentoOriginalNome,
    int AlimentoSubstitutoId,
    string AlimentoSubstitutoNome,
    decimal QuantidadeGramas,
    IReadOnlyList<string> RestricoesBloqueantes);

/// <summary>Motivo de pendência da versão adaptada (Spec 029, RF7 e RF8).</summary>
public sealed record PendenciaDeAdaptacao(
    int RefeicaoOrdem,
    string RefeicaoNome,
    int? AlimentoId,
    string? AlimentoNome,
    string Motivo);

/// <summary>Resultado puro da adaptação de um geral para uma escola e um conjunto (RF5 a RF7).</summary>
public sealed record ResultadoAdaptacao(
    IReadOnlyList<Refeicao> Refeicoes,
    IReadOnlyList<ItemSubstituido> Substituicoes,
    IReadOnlyList<PendenciaDeAdaptacao> Pendencias,
    StatusAdaptacao Status);

/// <summary>
/// Incompatibilidade entre um item do geral e um grupo da unidade, com a informação de que a
/// montagem manual precisa para avisar (com substituto) ou bloquear (sem substituto) — RF12.
/// </summary>
public sealed record IncompatibilidadeDoGeral(
    int EscolaId,
    string EscolaNome,
    ConjuntoDeRestricoes Conjunto,
    int RefeicaoOrdem,
    string RefeicaoNome,
    int AlimentoId,
    string AlimentoNome,
    IReadOnlyList<string> RestricoesBloqueantes,
    bool TemSubstitutoSeguro);

/// <summary>
/// Função pura de adaptação de cardápio (Spec 029, RF5 a RF7 e RF22; design "Algoritmo de
/// adaptação").
///
/// Recebe o geral já carregado, o catálogo de alimentos e as restrições com categoria, e
/// devolve as refeições adaptadas, as substituições e as pendências. Não consulta banco,
/// relógio nem serviço; toda escolha é determinística — candidatos ordenados pelo desvio
/// nutricional normalizado e, no empate, pelo identificador do alimento (RNF2).
///
/// Regras centrais:
/// - só substitui item cujo vínculo é **bloqueante** (alergia) com o conjunto; restrição
///   informativa não bloqueia nem gera substituição;
/// - o substituto é ativo, da mesma categoria e sem vínculo bloqueante com o conjunto;
/// - o substituto respeita ±20% de kcal e ±30% de proteína, lipídio e fibra do item;
/// - a refeição inteira respeita ±10% de kcal do geral;
/// - item bloqueante sem substituto é **removido** e vira pendência: a versão adaptada
///   nunca contém alimento bloqueante (RF22).
/// </summary>
public static class AdaptacaoDeCardapio
{
    public const decimal ToleranciaKcalDaRefeicao = 0.10m;
    public const decimal ToleranciaKcalDoItem = 0.20m;
    public const decimal ToleranciaMacroDoItem = 0.30m;

    public static ResultadoAdaptacao Adaptar(
        Cardapio geral,
        int escolaId,
        ConjuntoDeRestricoes conjunto,
        IReadOnlyList<Alimento> catalogo,
        IReadOnlyList<RestricaoDoCatalogo> restricoes)
    {
        ArgumentNullException.ThrowIfNull(geral);
        ArgumentNullException.ThrowIfNull(catalogo);
        ArgumentNullException.ThrowIfNull(restricoes);

        if (geral.Tipo != TipoCardapio.Geral)
        {
            throw new ArgumentException("Somente um cardápio geral pode ser adaptado.");
        }

        if (escolaId <= 0)
        {
            throw new ArgumentException("A adaptação exige uma escola válida.");
        }

        if (conjunto is null || conjunto.EstaVazio)
        {
            throw new ArgumentException("A adaptação exige um conjunto de restrições não vazio.");
        }

        var mapaDeRestricoes = restricoes.ToDictionary(restricao => restricao.Id);
        var alimentosPorId = catalogo.ToDictionary(alimento => alimento.Id);
        var refeicoes = new List<Refeicao>();
        var substituicoes = new List<ItemSubstituido>();
        var pendencias = new List<PendenciaDeAdaptacao>();

        foreach (var refeicao in geral.Refeicoes.OrderBy(refeicao => refeicao.Ordem))
        {
            var itensAdaptados = new List<ItemRefeicao>();
            var kcalDoGeral = 0m;
            var kcalDaAdaptada = 0m;

            foreach (var item in refeicao.Itens.OrderBy(item => item.AlimentoId))
            {
                if (!alimentosPorId.TryGetValue(item.AlimentoId, out var alimento))
                {
                    throw new InvalidOperationException(
                        $"O alimento {item.AlimentoId} do cardápio precisa estar no catálogo da adaptação.");
                }

                var valoresOriginais = CalculoNutricional.DoItem(alimento.Valores, item.QuantidadeGramas);
                kcalDoGeral += valoresOriginais.CaloriasKcal;

                var bloqueantes = RestricoesBloqueantes(alimento, conjunto, mapaDeRestricoes);

                if (bloqueantes.Count == 0)
                {
                    itensAdaptados.Add(new ItemRefeicao(alimento.Id, item.QuantidadeGramas));
                    kcalDaAdaptada += valoresOriginais.CaloriasKcal;
                    continue;
                }

                var substituto = EscolherSubstituto(
                    alimento,
                    item.QuantidadeGramas,
                    itensAdaptados.Select(itemAdaptado => itemAdaptado.AlimentoId),
                    catalogo,
                    conjunto,
                    mapaDeRestricoes);

                if (substituto is null)
                {
                    pendencias.Add(new PendenciaDeAdaptacao(
                        refeicao.Ordem,
                        refeicao.Nome,
                        alimento.Id,
                        alimento.Nome,
                        $"Sem substituto seguro da categoria '{alimento.Categoria?.Nome ?? alimento.CategoriaId.ToString()}' " +
                        $"para o conjunto {conjunto.Chave} (bloqueado por {string.Join(", ", bloqueantes)})."));

                    continue;
                }

                var valoresDoSubstituto = CalculoNutricional.DoItem(substituto.Valores, item.QuantidadeGramas);
                kcalDaAdaptada += valoresDoSubstituto.CaloriasKcal;

                itensAdaptados.Add(new ItemRefeicao(substituto.Id, item.QuantidadeGramas));

                substituicoes.Add(new ItemSubstituido(
                    refeicao.Ordem,
                    refeicao.Nome,
                    alimento.Id,
                    alimento.Nome,
                    substituto.Id,
                    substituto.Nome,
                    item.QuantidadeGramas,
                    bloqueantes));
            }

            refeicoes.Add(new Refeicao(refeicao.Nome, refeicao.Ordem, itensAdaptados));

            if (kcalDoGeral > 0m &&
                Math.Abs(kcalDaAdaptada - kcalDoGeral) > ToleranciaKcalDaRefeicao * kcalDoGeral)
            {
                pendencias.Add(new PendenciaDeAdaptacao(
                    refeicao.Ordem,
                    refeicao.Nome,
                    null,
                    null,
                    $"Refeição fora da tolerância de ±10% de kcal ({kcalDaAdaptada:0.##} kcal " +
                    $"contra {kcalDoGeral:0.##} kcal do geral)."));
            }
        }

        var status = pendencias.Count == 0
            ? StatusAdaptacao.Atualizado
            : StatusAdaptacao.PendenteDeRevisao;

        return new ResultadoAdaptacao(refeicoes, substituicoes, pendencias, status);
    }

    /// <summary>
    /// Verifica, antes de salvar o geral, quais itens são incompatíveis com cada grupo da
    /// unidade e se existe substituto seguro (RF12). É a mesma função de escolha usada na
    /// adaptação, para que a montagem manual nunca aceite um item que a adaptação recusaria.
    /// </summary>
    public static IReadOnlyList<IncompatibilidadeDoGeral> VerificarAdaptabilidade(
        Cardapio geral,
        IReadOnlyList<GrupoDeAdaptacao> grupos,
        IReadOnlyList<Alimento> catalogo,
        IReadOnlyList<RestricaoDoCatalogo> restricoes)
    {
        ArgumentNullException.ThrowIfNull(geral);
        ArgumentNullException.ThrowIfNull(grupos);
        ArgumentNullException.ThrowIfNull(catalogo);
        ArgumentNullException.ThrowIfNull(restricoes);

        var mapaDeRestricoes = restricoes.ToDictionary(restricao => restricao.Id);
        var alimentosPorId = catalogo.ToDictionary(alimento => alimento.Id);
        var achados = new List<IncompatibilidadeDoGeral>();

        foreach (var grupo in grupos.OrderBy(grupo => grupo.EscolaNome, StringComparer.Ordinal)
                     .ThenBy(grupo => grupo.Conjunto.Chave, StringComparer.Ordinal))
        {
            if (grupo.Conjunto.EstaVazio)
            {
                continue;
            }

            foreach (var refeicao in geral.Refeicoes.OrderBy(refeicao => refeicao.Ordem))
            {
                var alimentosDaRefeicao = refeicao.Itens
                    .Select(item => item.AlimentoId)
                    .ToHashSet();

                foreach (var item in refeicao.Itens.OrderBy(item => item.AlimentoId))
                {
                    if (!alimentosPorId.TryGetValue(item.AlimentoId, out var alimento))
                    {
                        throw new InvalidOperationException(
                            $"O alimento {item.AlimentoId} do cardápio precisa estar no catálogo da verificação.");
                    }

                    var bloqueantes = RestricoesBloqueantes(alimento, grupo.Conjunto, mapaDeRestricoes);

                    if (bloqueantes.Count == 0)
                    {
                        continue;
                    }

                    var substituto = EscolherSubstituto(
                        alimento,
                        item.QuantidadeGramas,
                        alimentosDaRefeicao,
                        catalogo,
                        grupo.Conjunto,
                        mapaDeRestricoes);

                    achados.Add(new IncompatibilidadeDoGeral(
                        grupo.EscolaId,
                        grupo.EscolaNome,
                        grupo.Conjunto,
                        refeicao.Ordem,
                        refeicao.Nome,
                        alimento.Id,
                        alimento.Nome,
                        bloqueantes,
                        substituto is not null));
                }
            }
        }

        return achados;
    }

    /// <summary>
    /// Nomes das restrições do conjunto que bloqueiam o alimento, na ordem canônica do
    /// conjunto. Uma restrição desconhecida no catálogo é tratada como bloqueante
    /// (fail-closed): ignorá-la poderia servir alimento inseguro.
    /// </summary>
    public static IReadOnlyList<string> RestricoesBloqueantes(
        Alimento alimento,
        ConjuntoDeRestricoes conjunto,
        IReadOnlyDictionary<int, RestricaoDoCatalogo> restricoes)
    {
        ArgumentNullException.ThrowIfNull(alimento);
        ArgumentNullException.ThrowIfNull(conjunto);
        ArgumentNullException.ThrowIfNull(restricoes);

        var nomes = new List<string>();

        foreach (var restricaoId in conjunto.Ids)
        {
            if (!alimento.PossuiVinculo(restricaoId))
            {
                continue;
            }

            var restricao = restricoes.TryGetValue(restricaoId, out var encontrada)
                ? encontrada
                : new RestricaoDoCatalogo(restricaoId, $"Restrição {restricaoId}", CategoriaRestricao.Alergia);

            if (SeveridadeDeRestricao.De(restricao.Categoria) != SeveridadeCompatibilidade.Bloqueante)
            {
                continue;
            }

            nomes.Add(restricao.Nome);
        }

        return nomes
            .Distinct(StringComparer.Ordinal)
            .OrderBy(nome => nome, StringComparer.Ordinal)
            .ToList();
    }

    private static Alimento? EscolherSubstituto(
        Alimento original,
        decimal gramas,
        IEnumerable<int> alimentosPresentesNaRefeicao,
        IReadOnlyList<Alimento> catalogo,
        ConjuntoDeRestricoes conjunto,
        IReadOnlyDictionary<int, RestricaoDoCatalogo> restricoes)
    {
        var presentes = alimentosPresentesNaRefeicao as ISet<int>
            ?? alimentosPresentesNaRefeicao.ToHashSet();

        return catalogo
            .Where(candidato => candidato.Ativo)
            .Where(candidato => candidato.Id != original.Id)
            .Where(candidato => candidato.CategoriaId == original.CategoriaId)
            .Where(candidato => !presentes.Contains(candidato.Id))
            .Where(candidato => RestricoesBloqueantes(candidato, conjunto, restricoes).Count == 0)
            .Where(candidato => DentroDaToleranciaDoItem(original, candidato, gramas))
            .OrderBy(candidato => DesvioNormalizado(original, candidato, gramas))
            .ThenBy(candidato => candidato.Id)
            .FirstOrDefault();
    }

    private static bool DentroDaToleranciaDoItem(Alimento original, Alimento candidato, decimal gramas)
    {
        var valoresOriginais = CalculoNutricional.DoItem(original.Valores, gramas);
        var valoresDoCandidato = CalculoNutricional.DoItem(candidato.Valores, gramas);

        return Dentro(valoresDoCandidato.CaloriasKcal, valoresOriginais.CaloriasKcal, ToleranciaKcalDoItem)
            && Dentro(valoresDoCandidato.ProteinasG, valoresOriginais.ProteinasG, ToleranciaMacroDoItem)
            && Dentro(valoresDoCandidato.GordurasG, valoresOriginais.GordurasG, ToleranciaMacroDoItem)
            && Dentro(valoresDoCandidato.FibrasG, valoresOriginais.FibrasG, ToleranciaMacroDoItem);
    }

    private static bool Dentro(decimal candidato, decimal original, decimal tolerancia) =>
        Math.Abs(candidato - original) <= tolerancia * original;

    private static decimal DesvioNormalizado(Alimento original, Alimento candidato, decimal gramas)
    {
        var valoresOriginais = CalculoNutricional.DoItem(original.Valores, gramas);
        var valoresDoCandidato = CalculoNutricional.DoItem(candidato.Valores, gramas);

        return Relativo(valoresOriginais.CaloriasKcal, valoresDoCandidato.CaloriasKcal)
            + Relativo(valoresOriginais.ProteinasG, valoresDoCandidato.ProteinasG)
            + Relativo(valoresOriginais.GordurasG, valoresDoCandidato.GordurasG)
            + Relativo(valoresOriginais.FibrasG, valoresDoCandidato.FibrasG);
    }

    /// <summary>
    /// Diferença relativa com zero tratado explicitamente: original zero exige candidato zero
    /// (desvio 0); candidato diferente de zero tem desvio 1. Evita divisão por zero sem
    /// introduzir aleatoriedade.
    /// </summary>
    private static decimal Relativo(decimal original, decimal candidato) =>
        original == 0m
            ? (candidato == 0m ? 0m : 1m)
            : Math.Abs(candidato - original) / original;
}
