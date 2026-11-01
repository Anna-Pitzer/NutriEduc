using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Cálculo nutricional (Spec 012, RF5): escala por gramas, arredonda por item com
/// `AwayFromZero` e soma os totais já arredondados. A decisão de arredondar por item
/// está fixada aqui para não mudar em silêncio.
/// </summary>
public class CalculoNutricionalTests
{
    private static readonly ValoresNutricionais Arroz =
        ValoresNutricionais.Criar(128, 2.5m, 28.1m, 0.2m, 1.6m, 1);

    [Fact]
    public void DoItem_Com30Gramas_EscalaPor100g()
    {
        var valores = CalculoNutricional.DoItem(Arroz, 30);

        Assert.Equal(38.4m, valores.CaloriasKcal);
        Assert.Equal(0.75m, valores.ProteinasG);
        Assert.Equal(8.43m, valores.CarboidratosG);
        Assert.Equal(0.06m, valores.GordurasG);
        Assert.Equal(0.48m, valores.FibrasG);
        Assert.Equal(0.3m, valores.SodioMg);
    }

    [Fact]
    public void DoItem_Com100Gramas_ReproduzAComposicao()
    {
        var valores = CalculoNutricional.DoItem(Arroz, 100);

        Assert.Equal(Arroz.CaloriasKcal, valores.CaloriasKcal);
        Assert.Equal(Arroz.ProteinasG, valores.ProteinasG);
        Assert.Equal(Arroz.CarboidratosG, valores.CarboidratosG);
    }

    [Fact]
    public void DoItem_Com250Gramas_EscalaParaDuasVezesEMeia()
    {
        var valores = CalculoNutricional.DoItem(Arroz, 250);

        Assert.Equal(320m, valores.CaloriasKcal);
        Assert.Equal(6.25m, valores.ProteinasG);
        Assert.Equal(70.25m, valores.CarboidratosG);
    }

    [Fact]
    public void DoItem_ArredondaAfastandoDoZero()
    {
        var valores = ValoresNutricionais.Criar(1.25m, 0, 0, 0, 0, 0);

        // 1,25 kcal por 100 g em 2 g = 0,025 → AwayFromZero vai para 0,03 (banker's daria 0,02).
        var item = CalculoNutricional.DoItem(valores, 2);

        Assert.Equal(0.03m, item.CaloriasKcal);
    }

    [Fact]
    public void Somar_DeDuasRefeicoes_SomaOsTotais()
    {
        var almoco = CalculoNutricional.DoItem(Arroz, 100);
        var jantar = CalculoNutricional.DoItem(Arroz, 50);

        var total = CalculoNutricional.Somar(new[] { almoco, jantar });

        Assert.Equal(192m, total.CaloriasKcal);
        Assert.Equal(3.75m, total.ProteinasG);
        // 1,6 g em 100 g + 0,8 g em 50 g = 2,4 g.
        Assert.Equal(2.4m, total.FibrasG);
    }

    [Fact]
    public void Somar_SemItens_DevolveZero()
    {
        var total = CalculoNutricional.Somar(Array.Empty<ValoresNutricionais>());

        Assert.Equal(0m, total.CaloriasKcal);
        Assert.Equal(0m, total.SodioMg);
    }
}
