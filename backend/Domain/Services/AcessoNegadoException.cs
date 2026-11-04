namespace Ntc.Domain.Services;

/// <summary>
/// Negação de acesso detectada pelo serviço de aplicação (Spec 028, RF8). O middleware
/// traduz para 403 ou 404; o corpo permanece `{ erro }`, sem dado pessoal.
/// </summary>
public sealed class AcessoNegadoException : Exception
{
    public AcessoNegadoException(bool naoEncontrado)
        : base(naoEncontrado ? "Recurso não encontrado." : "Acesso negado.")
    {
        NaoEncontrado = naoEncontrado;
    }

    public bool NaoEncontrado { get; }
}
