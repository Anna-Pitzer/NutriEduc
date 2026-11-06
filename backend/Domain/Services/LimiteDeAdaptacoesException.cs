namespace Ntc.Domain.Services;

/// <summary>
/// Teto de versões adaptadas por origem (Spec 029, D6). A exceção é lançada antes de
/// qualquer gravação para que a operação não deixe estado parcial; o middleware traduz em
/// 422 com a contagem.
/// </summary>
public sealed class LimiteDeAdaptacoesException : Exception
{
    public LimiteDeAdaptacoesException(int limite, int solicitadas)
        : base($"A origem já possui {solicitadas} versões adaptadas e o limite é {limite}.")
    {
        Limite = limite;
        Solicitadas = solicitadas;
    }

    public int Limite { get; }

    public int Solicitadas { get; }
}
