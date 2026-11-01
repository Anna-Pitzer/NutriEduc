namespace Ntc.Domain;

/// <summary>
/// Critério de consulta de aluno em vocabulário de negócio (Spec 024, F5): unidade, escola
/// e responsável. Não conhece sessão, papel nem claims — quem traduz o escopo do
/// solicitante para este record é a aplicação.
///
/// Campos nulos não filtram; <see cref="Todos"/> representa o escopo global.
/// </summary>
public sealed record FiltroAluno(
    int? UnidadeId = null,
    int? EscolaId = null,
    int? ResponsavelUsuarioId = null)
{
    public static FiltroAluno Todos { get; } = new();

    /// <summary>
    /// Filtro que não devolve aluno algum. Existe para escopo sem a referência necessária
    /// (merendeira sem unidade, escola sem escola) — silêncio, nunca vazamento.
    /// </summary>
    public static FiltroAluno Nenhum { get; } = new(UnidadeId: -1);
}
