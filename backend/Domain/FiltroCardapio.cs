using Ntc.Domain.Entity;

namespace Ntc.Domain;

/// <summary>
/// Filtro de consulta de cardápios (Spec 012, RF9 e RF10). Não contém palavra de
/// autorização: o recorte por papel é decidido no serviço, como em
/// <see cref="FiltroAlimento"/> (Spec 024, F5).
/// </summary>
/// <param name="UnidadeId">Unidade do cardápio; nulo lista todas (só para a nutrição).</param>
/// <param name="Data">Data que o período deve conter; nula não filtra por data.</param>
/// <param name="Situacao">Situação desejada; nula lista todas.</param>
/// <param name="Refeicao">Nome da refeição, comparado sem diferenciar caixa (RF10).</param>
public sealed record FiltroCardapio(
    int? UnidadeId = null,
    DateOnly? Data = null,
    SituacaoCardapio? Situacao = null,
    string? Refeicao = null);
