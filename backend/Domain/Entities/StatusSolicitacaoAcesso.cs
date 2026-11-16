namespace Ntc.Domain.Entity;

/// <summary>
/// Estado da solicitação de acesso (Spec 033, RF4/RF5). Transição monotônica a partir de
/// <see cref="Pendente"/>: aprovar ou recusar acontece uma única vez.
/// </summary>
public enum StatusSolicitacaoAcesso
{
    Pendente = 0,
    Aprovada = 1,
    Recusada = 2
}
