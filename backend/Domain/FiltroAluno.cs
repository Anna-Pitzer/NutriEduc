namespace Ntc.Domain;

/// <summary>
/// Critério de consulta de aluno em vocabulário de negócio (Spec 024, F5): unidade, escola,
/// responsável e busca textual. Não conhece sessão, papel nem claims — quem traduz o escopo
/// do solicitante para este record é a aplicação.
///
/// Campos nulos não filtram; <see cref="Todos"/> é ausência de recorte e NUNCA deve ser
/// usado como escopo de sessão. <see cref="Busca"/> e <see cref="Serie"/> apenas estreitam
/// o recorte já autorizado (Spec 034).
/// </summary>
public sealed record FiltroAluno(
    int? UnidadeId = null,
    int? EscolaId = null,
    int? ResponsavelUsuarioId = null,
    string? Busca = null,
    string? Serie = null)
{
    public static FiltroAluno Todos { get; } = new();

    /// <summary>
    /// Filtro que não devolve aluno algum. Existe para escopo sem a referência necessária
    /// (merendeira sem unidade, escola sem escola) — silêncio, nunca vazamento.
    /// </summary>
    public static FiltroAluno Nenhum { get; } = new(UnidadeId: -1);
}
