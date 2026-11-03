using Ntc.Domain.Entity;
using Ntc.Domain.Services;

namespace Ntc.Application;

/// <summary>
/// Mapeia o agregado <see cref="Cardapio"/> para os DTOs de listagem e detalhe. Extraído para
/// um único dono porque a Spec 015 (publicação, cancelamento e revisão) também devolve o
/// detalhe, e duplicar o cálculo nutricional violaria a regra de um só dono (L9).
/// </summary>
public static class CardapioMapper
{
    public static CardapioResumoDTO ParaResumo(Cardapio cardapio) => new()
    {
        Id = cardapio.Id,
        Nome = cardapio.Nome,
        UnidadeId = cardapio.UnidadeId,
        Situacao = cardapio.Situacao,
        Periodo = new PeriodoCardapioDTO(cardapio.Periodo),
        TotalRefeicoes = cardapio.Refeicoes.Count
    };

    public static CardapioDTO ParaDetalhe(Cardapio cardapio)
    {
        var refeicoes = new List<RefeicaoCardapioDTO>();
        var totaisPorRefeicao = new List<ValoresNutricionais>();

        foreach (var refeicao in cardapio.Refeicoes.OrderBy(refeicao => refeicao.Ordem))
        {
            var itens = new List<ItemCardapioDTO>();
            var valoresDosItens = new List<ValoresNutricionais>();

            foreach (var item in refeicao.Itens)
            {
                var valores = item.Alimento is null
                    ? ValoresNutricionais.Criar(0, 0, 0, 0, 0, 0)
                    : CalculoNutricional.DoItem(item.Alimento.Valores, item.QuantidadeGramas);

                valoresDosItens.Add(valores);

                itens.Add(new ItemCardapioDTO
                {
                    AlimentoId = item.AlimentoId,
                    Alimento = item.Alimento?.Nome ?? string.Empty,
                    Gramas = item.QuantidadeGramas,
                    Valores = new ValoresNutricionaisDTO(valores)
                });
            }

            var totalDaRefeicao = CalculoNutricional.Somar(valoresDosItens);
            totaisPorRefeicao.Add(totalDaRefeicao);

            refeicoes.Add(new RefeicaoCardapioDTO
            {
                Nome = refeicao.Nome,
                Ordem = refeicao.Ordem,
                Itens = itens,
                Total = new ValoresNutricionaisDTO(totalDaRefeicao)
            });
        }

        return new CardapioDTO
        {
            Id = cardapio.Id,
            Nome = cardapio.Nome,
            UnidadeId = cardapio.UnidadeId,
            Situacao = cardapio.Situacao,
            Periodo = new PeriodoCardapioDTO(cardapio.Periodo),
            CardapioDuplicadoDeId = cardapio.CardapioDuplicadoDeId,
            Refeicoes = refeicoes,
            Total = new ValoresNutricionaisDTO(CalculoNutricional.Somar(totaisPorRefeicao))
        };
    }
}
