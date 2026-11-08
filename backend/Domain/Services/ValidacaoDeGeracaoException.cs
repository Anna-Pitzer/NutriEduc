namespace Ntc.Domain.Services;

/// <summary>
/// O cardápio montado não passou nas invariantes de pós-montagem (Spec 032, RF7). Carrega a
/// lista de erros com código e mensagem; o middleware traduz para 422 e nada é gravado.
/// </summary>
public sealed class ValidacaoDeGeracaoException : Exception
{
    public IReadOnlyList<ErroDeValidacaoDaGeracao> Erros { get; }

    public ValidacaoDeGeracaoException(IReadOnlyList<ErroDeValidacaoDaGeracao> erros)
        : base("O cardápio gerado não passou na validação.")
    {
        Erros = erros;
    }
}
