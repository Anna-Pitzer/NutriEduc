using Ntc.Domain.Entity;

namespace Ntc.Domain;

/// <summary>
/// Filtro de consulta de feedbacks (Spec 018, RF5). Não contém palavra de autorização: o
/// recorte por papel é decidido no serviço e chega aqui já traduzido (Spec 024, F5).
/// </summary>
public sealed record FiltroFeedback(
    StatusFeedback? Status = null,
    int? UnidadeId = null,
    int? EscolaId = null,
    int? AutorId = null);
