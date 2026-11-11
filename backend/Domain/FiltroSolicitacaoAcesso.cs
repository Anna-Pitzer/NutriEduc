using Ntc.Domain.Entity;

namespace Ntc.Domain;

/// <summary>
/// Critério de consulta da fila de solicitações de acesso (Spec 033, RF6). Campos nulos
/// não filtram; a unidade é sempre imposta pelo serviço (RNF3), nunca derivada do payload.
/// </summary>
public sealed record FiltroSolicitacaoAcesso(
    StatusSolicitacaoAcesso? Status = null,
    TipoCadastroSolicitado? Tipo = null,
    string? Busca = null,
    int? UnidadeId = null);
