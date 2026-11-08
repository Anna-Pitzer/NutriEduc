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
        Tipo = cardapio.Tipo,
        OrigemId = cardapio.OrigemId,
        EscolaId = cardapio.EscolaId,
        StatusAdaptacao = cardapio.StatusAdaptacao,
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
            Tipo = cardapio.Tipo,
            OrigemId = cardapio.OrigemId,
            EscolaId = cardapio.EscolaId,
            ConjuntoRestricoesChave = cardapio.ConjuntoRestricoesChave,
            StatusAdaptacao = cardapio.StatusAdaptacao,
            Substituicoes = cardapio.Substituicoes.Select(ParaSubstituicao).ToList(),
            Pendencias = cardapio.Pendencias.Select(ParaPendencia).ToList(),
            Confirmacoes = cardapio.Confirmacoes.Select(ParaConfirmacao).ToList(),
            Nutricionista = ParaNutricionista(cardapio),
            Refeicoes = refeicoes,
            Total = new ValoresNutricionaisDTO(CalculoNutricional.Somar(totaisPorRefeicao))
        };
    }

    private static SubstituicaoCardapioDTO ParaSubstituicao(SubstituicaoDeItem substituicao) => new()
    {
        RefeicaoOrdem = substituicao.RefeicaoOrdem,
        RefeicaoNome = substituicao.RefeicaoNome,
        AlimentoOriginalId = substituicao.AlimentoOriginalId,
        AlimentoOriginal = substituicao.AlimentoOriginalNome,
        AlimentoSubstitutoId = substituicao.AlimentoSubstitutoId,
        AlimentoSubstituto = substituicao.AlimentoSubstitutoNome,
        Gramas = substituicao.QuantidadeGramas,
        RestricoesBloqueantes = substituicao.RestricoesBloqueantes
    };

    private static PendenciaAdaptacaoDTO ParaPendencia(PendenciaAdaptacao pendencia) => new()
    {
        RefeicaoOrdem = pendencia.RefeicaoOrdem,
        RefeicaoNome = pendencia.RefeicaoNome,
        AlimentoId = pendencia.AlimentoId,
        Alimento = pendencia.AlimentoNome,
        Motivo = pendencia.Motivo
    };

    private static ConfirmacaoIncompatibilidadeDTO ParaConfirmacao(
        ConfirmacaoDeIncompatibilidade confirmacao) => new()
    {
        Autor = confirmacao.AutorNome,
        CriadoEm = confirmacao.CriadoEmUtc,
        AlimentoId = confirmacao.AlimentoId,
        Alimento = confirmacao.AlimentoNome,
        Escolas = confirmacao.Escolas,
        Conjuntos = confirmacao.Conjuntos
    };

    /// <summary>
    /// Retrato da autoria (Spec 027). Cardápio sem responsável informado — anterior à regra —
    /// devolve nulo para o frontend não exibir autor inventado.
    /// </summary>
    private static NutricionistaDTO? ParaNutricionista(Cardapio cardapio) =>
        cardapio.NutricionistaId is null && cardapio.NutricionistaNome.Length == 0
            ? null
            : new NutricionistaDTO(
                cardapio.NutricionistaId ?? 0,
                cardapio.NutricionistaNome,
                cardapio.NutricionistaEmail,
                cardapio.NutricionistaCpf,
                cardapio.NutricionistaCrn,
                CrnDeNutricionista.EhValido(cardapio.NutricionistaCrn));
}
