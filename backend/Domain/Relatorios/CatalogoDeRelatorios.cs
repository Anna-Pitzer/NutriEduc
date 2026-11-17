namespace Ntc.Domain.Relatorios;

/// <summary>
/// Títulos do catálogo fechado (Spec 019, RF1). O tipo é a única chave aceita; nenhum
/// relatório fora daqui é executado.
/// </summary>
public static class CatalogoDeRelatorios
{
    public static string Titulo(TipoDeRelatorio tipo) => tipo switch
    {
        TipoDeRelatorio.Cardapios => "Cardápios",
        TipoDeRelatorio.AlunosRestricoes => "Alunos e restrições",
        TipoDeRelatorio.Ocorrencias => "Ocorrências",
        TipoDeRelatorio.SolicitacoesFeedbacks => "Solicitações e feedbacks",
        TipoDeRelatorio.IndicadoresAdministrativos => "Indicadores administrativos",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    /// <summary>Converte o texto da rota no tipo fechado; desconhecido devolve <c>null</c>.</summary>
    public static TipoDeRelatorio? Interpretar(string? valor)
    {
        if (!Enum.TryParse<TipoDeRelatorio>(valor, ignoreCase: true, out var tipo) ||
            !Enum.IsDefined(tipo))
        {
            return null;
        }

        return tipo;
    }
}
