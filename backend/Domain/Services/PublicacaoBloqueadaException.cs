namespace Ntc.Domain.Services;

/// <summary>Detalhe de um alimento incompatível no bloqueio de publicação (Spec 015, RF3).</summary>
public sealed record IncompatibilidadeDetalhe(string Alimento, string Restricao, string Refeicao);

/// <summary>
/// Publicação bloqueada por compatibilidade (Spec 015, RF3): a análise (Spec 014) encontrou ao
/// menos um bloqueante. O middleware traduz para 422 com <c>{ erro, detalhes }</c>, onde
/// <c>detalhes</c> é a lista de <see cref="IncompatibilidadeDetalhe"/>.
/// </summary>
public sealed class PublicacaoBloqueadaException : Exception
{
    public IReadOnlyList<IncompatibilidadeDetalhe> Detalhes { get; }

    public PublicacaoBloqueadaException(IReadOnlyList<IncompatibilidadeDetalhe> detalhes)
        : base("Cardápio incompatível com restrições de alunos.")
    {
        Detalhes = detalhes;
    }
}
