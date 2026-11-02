namespace Ntc.Domain.Entity;

/// <summary>
/// Composição nutricional de um alimento por 100 g (Spec 011, RF3).
///
/// Objeto de valor: não tem identidade própria — a identidade vem do
/// <see cref="Alimento"/> que o possui. É mapeado como tipo complexo no EF Core (L10),
/// não como entidade possuída, seguindo o mesmo padrão de <see cref="Turma"/>.
///
/// As seis medidas são obrigatórias e passam por <see cref="ValidacaoDeCampo.Medida"/>:
/// negativo ou mais de duas casas decimais lança <see cref="ArgumentException"/>.
/// A precisão é garantida aqui, não pelo SQLite (RNF1).
/// </summary>
public class ValoresNutricionais
{
    public decimal CaloriasKcal { get; private set; }

    public decimal ProteinasG { get; private set; }

    public decimal CarboidratosG { get; private set; }

    public decimal GordurasG { get; private set; }

    public decimal FibrasG { get; private set; }

    public decimal SodioMg { get; private set; }

    /// <summary>Somente para o EF Core materializar o tipo complexo. Não valida.</summary>
    private ValoresNutricionais()
    {
    }

    private ValoresNutricionais(
        decimal caloriasKcal,
        decimal proteinasG,
        decimal carboidratosG,
        decimal gordurasG,
        decimal fibrasG,
        decimal sodioMg)
    {
        CaloriasKcal = caloriasKcal;
        ProteinasG = proteinasG;
        CarboidratosG = carboidratosG;
        GordurasG = gordurasG;
        FibrasG = fibrasG;
        SodioMg = sodioMg;
    }

    /// <summary>
    /// Cria a composição validando as seis medidas antes de atribuir qualquer uma delas.
    /// </summary>
    public static ValoresNutricionais Criar(
        decimal caloriasKcal,
        decimal proteinasG,
        decimal carboidratosG,
        decimal gordurasG,
        decimal fibrasG,
        decimal sodioMg)
    {
        return new ValoresNutricionais(
            ValidacaoDeCampo.Medida(caloriasKcal, "Calorias (kcal)"),
            ValidacaoDeCampo.Medida(proteinasG, "Proteínas (g)"),
            ValidacaoDeCampo.Medida(carboidratosG, "Carboidratos (g)"),
            ValidacaoDeCampo.Medida(gordurasG, "Gorduras (g)"),
            ValidacaoDeCampo.Medida(fibrasG, "Fibras (g)"),
            ValidacaoDeCampo.Medida(sodioMg, "Sódio (mg)"));
    }
}
