namespace Ntc.Domain.Relatorios;

/// <summary>
/// Filtro de relatório já normalizado (Spec 019, RF2). O serviço resolve o recorte de
/// escopo antes de construir este objeto; o repositório só aplica o que recebe.
/// </summary>
public sealed record FiltroDeRelatorio(
    DateOnly De,
    DateOnly Ate,
    int? UnidadeId,
    int? EscolaId)
{
    public const int IntervaloMaximoDias = 366;
    public const int JanelaPadraoDias = 30;

    /// <summary>Valida o intervalo informado pelo cliente, sem inventar default.</summary>
    public static string? Validar(DateOnly? de, DateOnly? ate)
    {
        if (de is { } inicio && ate is { } fim)
        {
            if (inicio > fim)
            {
                return "A data inicial não pode ser posterior à final.";
            }

            if (fim.DayNumber - inicio.DayNumber > IntervaloMaximoDias)
            {
                return $"O intervalo máximo é de {IntervaloMaximoDias} dias.";
            }
        }

        return null;
    }

    /// <summary>
    /// Regra de default documentada: sem período, usa os últimos 30 dias; só início usa
    /// hoje como fim; só fim usa 30 dias antes como início.
    /// </summary>
    public static FiltroDeRelatorio Normalizar(
        DateOnly? de,
        DateOnly? ate,
        DateOnly hoje,
        int? unidadeId,
        int? escolaId)
    {
        var fim = ate ?? hoje;
        var inicio = de ?? fim.AddDays(-JanelaPadraoDias);

        return new FiltroDeRelatorio(inicio, fim, unidadeId, escolaId);
    }
}
