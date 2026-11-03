namespace Ntc.Domain.Services;

/// <summary>
/// Sinaliza que uma refeição não pôde ser gerada com os parâmetros e candidatos fornecidos
/// (Spec 013, RF5 e RF6). Carrega a refeição e o motivo, que o middleware de erro traduz
/// para 422 com <c>{ erro, detalhes }</c> — nenhum cardápio parcial é salvo.
/// </summary>
public sealed class GeracaoImpossivelException : Exception
{
    public string Refeicao { get; }

    public string Motivo { get; }

    public GeracaoImpossivelException(string refeicao, string motivo)
        : base($"Não foi possível gerar a refeição '{refeicao}'.")
    {
        Refeicao = refeicao;
        Motivo = motivo;
    }
}
