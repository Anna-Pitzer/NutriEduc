using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>Erro de validação da geração, com código estável para o contrato (Spec 032, RF7).</summary>
public sealed record ErroDeValidacaoDaGeracao(string Codigo, string Mensagem);

/// <summary>
/// Invariantes pós-montagem do cardápio gerado (Spec 032, RF7).
///
/// Revalida o <see cref="Cardapio"/> montado — não o resultado parcial do gerador — e
/// recalcula o total calórico pelo mesmo <see cref="CalculoNutricional"/> da Spec 012. O valor
/// persistido nunca é copiado do gerador. É função pura: o catálogo entra por parâmetro para
/// resolver categoria e composição de cada item.
/// </summary>
public static class ValidadorDeCardapioGerado
{
    public static IReadOnlyList<ErroDeValidacaoDaGeracao> Validar(
        Cardapio cardapio,
        ParametroGeracao parametros,
        IReadOnlyList<Alimento> catalogo)
    {
        var erros = new List<ErroDeValidacaoDaGeracao>();
        var porId = catalogo.ToDictionary(alimento => alimento.Id);

        if (cardapio.Periodo.Fim < cardapio.Periodo.Inicio)
            erros.Add(new ErroDeValidacaoDaGeracao("periodo_invalido", "O fim do período precede o início."));

        if (cardapio.Refeicoes.Count != parametros.RefeicoesPadrao.Count)
            erros.Add(new ErroDeValidacaoDaGeracao(
                "refeicoes_incompletas", "A quantidade de refeições não corresponde à configuração."));

        var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var refeicao in cardapio.Refeicoes)
        {
            if (!nomes.Add(refeicao.Nome))
                erros.Add(new ErroDeValidacaoDaGeracao("refeicao_duplicada", $"A refeição '{refeicao.Nome}' está repetida."));

            var ids = refeicao.Itens.Select(item => item.AlimentoId).ToList();

            if (ids.Count != parametros.OpcoesPorRefeicao || ids.Distinct().Count() != ids.Count)
                erros.Add(new ErroDeValidacaoDaGeracao(
                    "itens_insuficientes",
                    $"A refeição '{refeicao.Nome}' não tem exatamente {parametros.OpcoesPorRefeicao} itens distintos."));

            var categorias = new HashSet<int>();
            var kcal = 0m;

            foreach (var item in refeicao.Itens)
            {
                if (item.AlimentoId <= 0 || !porId.TryGetValue(item.AlimentoId, out var alimento))
                {
                    erros.Add(new ErroDeValidacaoDaGeracao(
                        "item_invalido", $"A refeição '{refeicao.Nome}' contém um item com alimento inválido."));
                    continue;
                }

                categorias.Add(alimento.CategoriaId);
                kcal += CalculoNutricional.DoItem(alimento.Valores, item.QuantidadeGramas).CaloriasKcal;
            }

            foreach (var categoriaId in parametros.CategoriasObrigatorias)
            {
                if (!categorias.Contains(categoriaId))
                    erros.Add(new ErroDeValidacaoDaGeracao(
                        "categoria_ausente", $"A categoria obrigatória {categoriaId} está ausente na refeição '{refeicao.Nome}'."));
            }

            if (kcal < parametros.CaloriasMinimasRefeicao || kcal > parametros.CaloriasMaximasRefeicao)
                erros.Add(new ErroDeValidacaoDaGeracao(
                    "faixa_calorica", $"A refeição '{refeicao.Nome}' tem {kcal:0.##} kcal, fora da faixa configurada."));
        }

        if (cardapio.UnidadeId <= 0)
            erros.Add(new ErroDeValidacaoDaGeracao("unidade_invalida", "O cardápio exige uma unidade válida."));

        return erros;
    }
}
