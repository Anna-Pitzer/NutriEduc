namespace Ntc.Domain.Entity;

/// <summary>Ciclo de vida da solicitação (Spec 017, RF4).</summary>
public enum StatusSolicitacao
{
    Aberta,
    EmAnalise,
    Aprovada,
    Recusada,
    Concluida,
    Cancelada
}
