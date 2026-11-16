using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>Alimento candidato à geração (Spec 013): id, categoria e composição por 100 g.</summary>
public sealed record CandidatoAlimento(int Id, int CategoriaId, ValoresNutricionais Valores);

/// <summary>Refeição padrão da unidade, com a ordem em que aparece no cardápio (Spec 013).</summary>
public sealed record RefeicaoPadrao(string Nome, int Ordem);

/// <summary>Item selecionado para uma refeição gerada (Spec 013, RF4).</summary>
public sealed record ItemGerado(int AlimentoId, int CategoriaId, ValoresNutricionais Valores);

/// <summary>Entrada pura do gerador: nada de banco, relógio ou EF chega até aqui (RNF1).</summary>
public sealed record EntradaGeracao(
    IReadOnlyList<RefeicaoPadrao> Refeicoes,
    IReadOnlyList<CandidatoAlimento> Candidatos,
    ParametroGeracao Parametros,
    IReadOnlyCollection<int> AlimentosBloqueados,
    int Seed);

/// <summary>
/// Resultado da geração: refeições com itens, a semente usada, os avisos e as tentativas
/// consumidas por refeição (Spec 013, RF10; Spec 032, RF12).
/// </summary>
public sealed record ResultadoGeracao(
    IReadOnlyList<(RefeicaoPadrao Refeicao, IReadOnlyList<ItemGerado> Itens)> Refeicoes,
    int Seed,
    IReadOnlyList<string> Avisos,
    IReadOnlyList<TentativaRefeicao> TentativasPorRefeicao);
