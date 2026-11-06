using Ntc.Domain.Entity;

namespace Ntc.Domain;

/// <summary>
/// Filtro de consulta de solicitações (Spec 017, RF8). Não contém palavra de autorização:
/// o recorte por papel é decidido no serviço e chega aqui já traduzido (Spec 024, F5).
/// </summary>
public sealed record FiltroSolicitacao(
    TipoSolicitacao? Tipo = null,
    StatusSolicitacao? Status = null,
    int? UnidadeId = null,
    int? EscolaId = null,
    int? AlunoId = null,
    int? CardapioId = null,
    DateTime? De = null,
    DateTime? Ate = null,
    int? SolicitanteId = null,
    int? ResponsavelUsuarioId = null,
    IReadOnlyCollection<TipoSolicitacao>? TiposExcluidos = null);
