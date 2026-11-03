namespace Ntc.Domain.Services;

/// <summary>
/// Já existe cardápio publicado no período (Spec 015, RF4). O middleware traduz para 409 com
/// <c>{ erro, detalhes }</c>, onde <c>detalhes</c> carrega o id do cardápio conflitante.
/// </summary>
public sealed class PublicacaoSobrepostaException : Exception
{
    public int CardapioId { get; }

    public PublicacaoSobrepostaException(int cardapioId)
        : base("Já existe cardápio publicado no período.")
    {
        CardapioId = cardapioId;
    }
}
