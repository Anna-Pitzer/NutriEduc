namespace Ntc.Domain.Services;

/// <summary>
/// Transição de situação não prevista no ciclo de vida do cardápio (Spec 015, RF1). Herda de
/// <see cref="InvalidOperationException"/>, que o middleware de erro traduz para 409.
/// </summary>
public sealed class TransicaoInvalidaException : InvalidOperationException
{
    public TransicaoInvalidaException(string mensagem)
        : base(mensagem)
    {
    }
}
