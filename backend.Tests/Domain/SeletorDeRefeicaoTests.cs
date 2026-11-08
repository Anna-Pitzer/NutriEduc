using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Seleção com tentativas e diagnóstico por refeição (Spec 032, RF5/RF13/RF15).
/// </summary>
public class SeletorDeRefeicaoTests
{
    private static CandidatoAlimento Alimento(int id, int categoriaId, decimal calorias) =>
        new(id, categoriaId, ValoresNutricionais.Criar(calorias, 0, 0, 0, 0, 0));

    private static ParametroGeracao Parametros(
        decimal min,
        decimal max,
        int opcoes,
        int[]? categorias = null) =>
        new(1, min, max, opcoes, 0, false, categorias, new[] { "Almoço" });

    private static readonly RefeicaoPadrao Refeicao = new("Almoço", 1);

    [Fact]
    public void TentarRefeicao_MesmoSeed_DevolveMesmosItens()
    {
        var pool = Enumerable.Range(1, 5)
            .Select(i => Alimento(i, 1, 100m))
            .ToList();

        var primeiro = SeletorDeRefeicao.TentarRefeicao(
            Refeicao, pool, Parametros(0, 600, 2), 42, out _);
        var segundo = SeletorDeRefeicao.TentarRefeicao(
            Refeicao, pool, Parametros(0, 600, 2), 42, out _);

        Assert.Equal(
            primeiro!.Select(item => item.AlimentoId),
            segundo!.Select(item => item.AlimentoId));
    }

    [Fact]
    public void TentarRefeicao_FaixaImpossivel_DevolveDiagnosticoComFaixaAlcancavel()
    {
        var pool = new[]
        {
            Alimento(1, 1, 100m),
            Alimento(2, 1, 200m)
        };

        var itens = SeletorDeRefeicao.TentarRefeicao(
            Refeicao, pool, Parametros(400, 500, 2), 1, out var diagnostico);

        Assert.Null(itens);
        Assert.Equal(SeletorDeRefeicao.TentativasMaximas, diagnostico.Tentativas);
        Assert.Equal(2, diagnostico.CandidatosDisponiveis);
        Assert.Null(diagnostico.CategoriaAusente);
        // Faixa máxima alcançável com o catálogo é 100 + 200 = 300, abaixo do mínimo de 400.
        Assert.True(diagnostico.FaixaMaximaAlcancavel <= 300m);
    }

    [Fact]
    public void TentarRefeicao_CategoriaAusente_DevolveCategoria()
    {
        var pool = new[] { Alimento(1, 1, 100m) };

        var itens = SeletorDeRefeicao.TentarRefeicao(
            Refeicao, pool, Parametros(0, 600, 1, new[] { 99 }), 1, out var diagnostico);

        Assert.Null(itens);
        Assert.Equal(99, diagnostico.CategoriaAusente);
    }

    [Fact]
    public void TentarRefeicao_CandidatosInsuficientes_NaoDevolveItens()
    {
        var pool = new[] { Alimento(1, 1, 100m) };

        var itens = SeletorDeRefeicao.TentarRefeicao(
            Refeicao, pool, Parametros(0, 600, 3), 1, out var diagnostico);

        Assert.Null(itens);
        Assert.Equal(1, diagnostico.CandidatosDisponiveis);
    }
}
