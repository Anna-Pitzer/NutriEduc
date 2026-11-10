using Ntc.Domain.Entity;

namespace Ntc.Domain;

/// <summary>
/// Filtro de consulta de ocorrências (Spec 016, RF7). Não contém palavra de autorização:
/// o recorte por papel é decidido no serviço e chega aqui já traduzido (Spec 024, F5).
/// </summary>
public sealed record FiltroOcorrencia(
    TipoOcorrencia? Tipo = null,
    StatusOcorrencia? Status = null,
    int? UnidadeId = null,
    int? EscolaId = null,
    int? AlunoId = null,
    DateTime? De = null,
    DateTime? Ate = null,
    int? ResponsavelUsuarioId = null);
