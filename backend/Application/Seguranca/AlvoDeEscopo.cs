namespace Ntc.Application.Seguranca;

/// <summary>
/// Alvo de uma decisão de acesso com os identificadores institucionais já resolvidos por
/// consulta (Spec 028, T3). Nenhum controller monta este objeto a partir de payload: ele
/// vem do registro carregado antes da escrita (invariante I5).
/// </summary>
public sealed record AlvoDeEscopo(
    int? UnidadeId,
    int? EscolaId,
    bool VinculadoAoResponsavel = false)
{
    /// <summary>Alvo sem fronteira institucional (dado de referência ou ação global).</summary>
    public static AlvoDeEscopo SemFronteira { get; } = new(null, null);
}
