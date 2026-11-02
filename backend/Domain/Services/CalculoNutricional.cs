using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>
/// Cálculo nutricional do cardápio (Spec 012, RF5).
///
/// Função pura: recebe a composição já carregada, não consulta banco e não conhece EF.
/// É a única implementação do cálculo — os endpoints e o frontend consomem este
/// resultado em vez de repetir a regra (RNF3).
///
/// Decisão registrada: arredonda por item e depois soma. Arredondar a soma total daria
/// outro número, e a diferença não pode mudar em silêncio.
/// </summary>
public static class CalculoNutricional
{
    /// <summary>Composição do item servido: valores por 100 g escalados pelas gramas.</summary>
    public static ValoresNutricionais DoItem(ValoresNutricionais por100g, decimal gramas)
        => ValoresNutricionais.Criar(
            Arredondar(por100g.CaloriasKcal * gramas / 100m),
            Arredondar(por100g.ProteinasG * gramas / 100m),
            Arredondar(por100g.CarboidratosG * gramas / 100m),
            Arredondar(por100g.GordurasG * gramas / 100m),
            Arredondar(por100g.FibrasG * gramas / 100m),
            Arredondar(por100g.SodioMg * gramas / 100m));

    /// <summary>
    /// Soma composições já arredondadas. A soma de valores com duas casas continua com
    /// duas casas, então não há novo arredondamento aqui.
    /// </summary>
    public static ValoresNutricionais Somar(IEnumerable<ValoresNutricionais> valores)
    {
        var lista = valores.ToList();

        return ValoresNutricionais.Criar(
            lista.Sum(valor => valor.CaloriasKcal),
            lista.Sum(valor => valor.ProteinasG),
            lista.Sum(valor => valor.CarboidratosG),
            lista.Sum(valor => valor.GordurasG),
            lista.Sum(valor => valor.FibrasG),
            lista.Sum(valor => valor.SodioMg));
    }

    private static decimal Arredondar(decimal valor)
        => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
