namespace Ntc.Domain.Entity;

/// <summary>
/// Período de vigência de um cardápio (Spec 012, RF1).
///
/// Objeto de valor sem identidade, mapeado como tipo complexo (L10) — colunas
/// `Periodo_Inicio` e `Periodo_Fim`. Por isso o construtor sem parâmetros e os setters
/// privados: o materializador do EF precisa de ambos, e um `record` só de leitura não
/// funciona aqui.
///
/// O limite é de 366 dias **inclusive**: de 01/01 a 31/12 de um ano bissexto é aceito.
/// </summary>
public class PeriodoVigencia
{
    public const int DiasMaximos = 366;

    public DateOnly Inicio { get; private set; }

    public DateOnly Fim { get; private set; }

    /// <summary>Somente para o EF Core materializar o tipo complexo. Não valida.</summary>
    private PeriodoVigencia()
    {
    }

    public PeriodoVigencia(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
        {
            throw new ArgumentException("O fim do período não pode ser anterior ao início.");
        }

        if (fim.DayNumber - inicio.DayNumber + 1 > DiasMaximos)
        {
            throw new ArgumentException($"O período não pode exceder {DiasMaximos} dias.");
        }

        Inicio = inicio;
        Fim = fim;
    }

    /// <summary>True quando a data está dentro do período, incluindo as bordas.</summary>
    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;
}
