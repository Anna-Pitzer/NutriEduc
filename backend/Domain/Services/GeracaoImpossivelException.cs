namespace Ntc.Domain.Services;

/// <summary>
/// Sinaliza que nenhuma refeição pôde ser gerada com os parâmetros e candidatos fornecidos
/// (Spec 013, RF5/RF6; Spec 032, RF15). Carrega o diagnóstico por refeição, que o middleware
/// de erro traduz para 422 — nenhum cardápio parcial é salvo.
/// </summary>
public sealed class GeracaoImpossivelException : Exception
{
    public IReadOnlyList<DiagnosticoRefeicao> Diagnostico { get; }

    public GeracaoImpossivelException(IReadOnlyList<DiagnosticoRefeicao> diagnostico)
        : base("Não foi possível gerar o cardápio.")
    {
        Diagnostico = diagnostico;
    }
}
